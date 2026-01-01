#!/usr/bin/env python3
"""
Build script for Flowbite Blazor Riddle DM-Assistant
Supports: build, publish, watch, run, start, stop, status commands
"""

import sys
import os
import platform
import subprocess
import urllib.request
import signal
import time
import psutil
from pathlib import Path
from typing import Optional, Dict

import sqlite3
import re

REQUIRED_DOTNET_VERSION = "9.0"
TAILWIND_VERSION = "v3.4.15"
TOOLS_DIR = Path("src/Riddle.Web/tools")
DOTNET_DIR = Path("./dotnet")
SOLUTION_PATH = "Riddle.slnx"
PROJECT_PATH = "src/Riddle.Web/Riddle.Web.csproj"
PID_FILE = Path(".riddle.pid")
LOG_FILE = Path("riddle.log")
DB_PATH = Path("src/Riddle.Web/riddle.db")
BACKUP_DIR = Path("backups")


def get_os_info() -> Dict[str, str]:
    """Detect OS and return tailwindcss download info"""
    system = platform.system()
    
    if system == "Linux":
        return {
            "url": f"https://github.com/tailwindlabs/tailwindcss/releases/download/{TAILWIND_VERSION}/tailwindcss-linux-x64",
            "exec_name": "tailwindcss",
            "os_name": "Linux"
        }
    elif system == "Darwin":
        return {
            "url": f"https://github.com/tailwindlabs/tailwindcss/releases/download/{TAILWIND_VERSION}/tailwindcss-macos-arm64",
            "exec_name": "tailwindcss",
            "os_name": "macOS"
        }
    elif system == "Windows":
        return {
            "url": f"https://github.com/tailwindlabs/tailwindcss/releases/download/{TAILWIND_VERSION}/tailwindcss-windows-x64.exe",
            "exec_name": "tailwindcss.exe",
            "os_name": "Windows"
        }
    else:
        print(f"Unsupported OS: {system}")
        sys.exit(1)


def setup_tailwindcss() -> None:
    """Check and download Tailwind CSS if needed"""
    os_info = get_os_info()
    tailwind_path = TOOLS_DIR / os_info["exec_name"]
    
    if tailwind_path.exists():
        print(f"Tailwind CSS executable already exists at {tailwind_path}")
        return
    
    print(f"Downloading Tailwind CSS executable for {os_info['os_name']}...")
    TOOLS_DIR.mkdir(parents=True, exist_ok=True)
    
    try:
        urllib.request.urlretrieve(os_info["url"], tailwind_path)
        
        # Make executable on Unix-like systems
        if platform.system() != "Windows":
            os.chmod(tailwind_path, 0o755)
        
        print(f"Tailwind CSS executable downloaded to {tailwind_path}")
    except Exception as e:
        print(f"Error downloading Tailwind CSS: {e}")
        sys.exit(1)


def get_dotnet_version() -> Optional[str]:
    """Get installed dotnet version, return None if not found"""
    try:
        result = subprocess.run(
            ["dotnet", "--version"],
            capture_output=True,
            text=True,
            check=True
        )
        return result.stdout.strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        return None


def version_greater_equal(current: str, required: str) -> bool:
    """Compare version numbers (major.minor)"""
    try:
        current_parts = [int(x) for x in current.split('.')[:2]]
        required_parts = [int(x) for x in required.split('.')[:2]]
        
        # Compare major version first, then minor
        if current_parts[0] > required_parts[0]:
            return True
        elif current_parts[0] == required_parts[0]:
            return current_parts[1] >= required_parts[1]
        else:
            return False
    except (ValueError, IndexError):
        # Fallback to string comparison if parsing fails
        return current >= required


def check_dotnet() -> Optional[str]:
    """Check if dotnet is installed and meets version requirements"""
    dotnet_version = get_dotnet_version()
    
    if dotnet_version:
        print(f"Found .NET version: {dotnet_version}")
        
        # Extract major.minor version
        version_parts = dotnet_version.split('.')[:2]
        current_version = '.'.join(version_parts)
        
        if version_greater_equal(current_version, REQUIRED_DOTNET_VERSION):
            print(f"Using system-installed .NET {dotnet_version}")
            return "dotnet"
        else:
            print(f"System .NET version {current_version} is older than required version {REQUIRED_DOTNET_VERSION}")
            return None
    else:
        print("No system .NET installation found")
        return None


def install_dotnet() -> str:
    """Install .NET SDK locally"""
    print(f"Installing .NET {REQUIRED_DOTNET_VERSION}...")
    
    try:
        if platform.system() == "Windows":
            # Use PowerShell on Windows
            install_script = "dotnet-install.ps1"
            urllib.request.urlretrieve(
                "https://dot.net/v1/dotnet-install.ps1",
                install_script
            )
            
            subprocess.run(
                ["powershell", "-ExecutionPolicy", "Bypass", "-File", install_script,
                 "-Channel", REQUIRED_DOTNET_VERSION, "-InstallDir", str(DOTNET_DIR)],
                check=True
            )
        else:
            # Use bash script on Unix-like systems
            install_script = "dotnet-install.sh"
            urllib.request.urlretrieve(
                "https://dot.net/v1/dotnet-install.sh",
                install_script
            )
            os.chmod(install_script, 0o755)
            
            subprocess.run(
                [f"./{install_script}", "-c", REQUIRED_DOTNET_VERSION, "-InstallDir", str(DOTNET_DIR)],
                check=True
            )
        
        dotnet_path = DOTNET_DIR / ("dotnet.exe" if platform.system() == "Windows" else "dotnet")
        
        # Verify installation
        result = subprocess.run(
            [str(dotnet_path), "--version"],
            capture_output=True,
            text=True,
            check=True
        )
        print(f"Using .NET version: {result.stdout.strip()}")
        
        return str(dotnet_path)
    
    except Exception as e:
        print(f"Error installing .NET: {e}")
        sys.exit(1)


def kill_test_processes() -> int:
    """Kill any lingering test host processes from previous test runs"""
    killed = 0
    for proc in psutil.process_iter(['pid', 'name', 'cmdline']):
        try:
            cmdline = ' '.join(proc.info['cmdline'] or [])
            if 'testhost' in cmdline.lower() or 'Riddle.Web.IntegrationTests' in cmdline:
                print(f"  Killing stale test process {proc.pid}: {proc.info['name']}")
                proc.kill()
                killed += 1
        except (psutil.NoSuchProcess, psutil.AccessDenied):
            pass
    return killed


def is_process_running(pid: int) -> bool:
    """Check if a process with given PID is running"""
    try:
        process = psutil.Process(pid)
        return process.is_running() and process.status() != psutil.STATUS_ZOMBIE
    except (psutil.NoSuchProcess, psutil.AccessDenied):
        return False


def get_running_pid() -> Optional[int]:
    """Get PID of running application if it exists"""
    if not PID_FILE.exists():
        return None
    
    try:
        with open(PID_FILE, 'r') as f:
            pid = int(f.read().strip())
        
        if is_process_running(pid):
            return pid
        else:
            # Clean up stale PID file
            PID_FILE.unlink()
            return None
    except (ValueError, IOError):
        return None


def check_status() -> None:
    """Check if application is running"""
    pid = get_running_pid()
    
    if pid:
        print(f"✓ Application is running (PID: {pid})")
        print(f"  URL: http://localhost:5000")
        print(f"  Log file: {LOG_FILE}")
    else:
        print("✗ Application is not running")


def start_background(dotnet_path: str) -> None:
    """Start application in background"""
    # Check if already running
    existing_pid = get_running_pid()
    if existing_pid:
        print(f"Application is already running (PID: {existing_pid})")
        print("Use 'python build.py stop' to stop it first")
        return
    
    print("Starting application in background...")
    
    # Open log file
    log_file = open(LOG_FILE, 'w')
    
    # Set environment for Development mode
    env = os.environ.copy()
    env["ASPNETCORE_ENVIRONMENT"] = "Development"
    
    # Start process
    if platform.system() == "Windows":
        # On Windows, use CREATE_NEW_PROCESS_GROUP to prevent Ctrl+C propagation
        process = subprocess.Popen(
            [dotnet_path, "run", "--project", PROJECT_PATH, "--no-restore"],
            stdout=log_file,
            stderr=subprocess.STDOUT,
            env=env,
            creationflags=subprocess.CREATE_NEW_PROCESS_GROUP
        )
    else:
        # On Unix, use start_new_session
        process = subprocess.Popen(
            [dotnet_path, "run", "--project", PROJECT_PATH, "--no-restore"],
            stdout=log_file,
            stderr=subprocess.STDOUT,
            env=env,
            start_new_session=True
        )
    
    # Save PID
    with open(PID_FILE, 'w') as f:
        f.write(str(process.pid))
    
    print(f"Application started (PID: {process.pid})")
    print(f"Log output: {LOG_FILE}")
    
    # Wait a few seconds and check if it's still running
    time.sleep(3)
    
    if is_process_running(process.pid):
        print("✓ Application is running at http://localhost:5000")
    else:
        print("✗ Application failed to start. Check log file for details:")
        print(f"  tail {LOG_FILE}")


def stop_background() -> None:
    """Stop background application"""
    pid = get_running_pid()
    
    if not pid:
        print("No running application found")
        return
    
    print(f"Stopping application (PID: {pid})...")
    
    try:
        process = psutil.Process(pid)
        
        # Try graceful shutdown first
        if platform.system() == "Windows":
            process.terminate()
        else:
            process.send_signal(signal.SIGTERM)
        
        # Wait up to 10 seconds for graceful shutdown
        try:
            process.wait(timeout=10)
            print("✓ Application stopped gracefully")
        except psutil.TimeoutExpired:
            # Force kill if still running
            print("Application did not stop gracefully, forcing shutdown...")
            process.kill()
            process.wait(timeout=5)
            print("✓ Application stopped forcefully")
        
        # Clean up PID file
        if PID_FILE.exists():
            PID_FILE.unlink()
    
    except psutil.NoSuchProcess:
        print("Process already stopped")
        if PID_FILE.exists():
            PID_FILE.unlink()
    
    except Exception as e:
        print(f"Error stopping application: {e}")
        sys.exit(1)


def run_dotnet_command(dotnet_path: str, command: str) -> None:
    """Execute the appropriate dotnet command"""
    try:
        if command == "publish":
            print("Publishing project to ./dist...")
            subprocess.run(
                [dotnet_path, "publish", PROJECT_PATH, "-c", "Release", "-o", "dist"],
                check=True
            )
            print("Successfully published to ./dist")
        
        elif command == "watch":
            print("Starting project with hot reload...")
            print("Press Ctrl+C to stop watching...")

            # Set environment for Development mode
            env = os.environ.copy()
            env["ASPNETCORE_ENVIRONMENT"] = "Development"

            subprocess.run(
                [dotnet_path, "watch", "--project", PROJECT_PATH, "--no-restore", "-c", "Development"],
                env=env
            )
        
        elif command == "run":
            print("Running project...")
            print("Press Ctrl+C to stop...")
            subprocess.run(
                [dotnet_path, "run", "--project", PROJECT_PATH, "--no-restore"]
            )
        
        elif command == "build":
            # Auto-stop any running application to prevent file lock issues
            pid = get_running_pid()
            if pid:
                print("Stopping running application before build...")
                stop_background()
            
            print("Building solution (web app + tests)...")
            subprocess.run(
                [dotnet_path, "build", SOLUTION_PATH],
                check=True
            )
            print("Successfully built solution")
        
        elif command == "start":
            # Auto-stop any running application to prevent file lock issues
            pid = get_running_pid()
            if pid:
                print("Stopping running application before build...")
                stop_background()
            
            # Auto-build solution before starting
            print("Building solution before start...")
            subprocess.run(
                [dotnet_path, "build", SOLUTION_PATH],
                check=True
            )
            start_background(dotnet_path)
        
        elif command == "test":
            # Auto-stop any running application to prevent port conflicts
            pid = get_running_pid()
            if pid:
                print("Stopping running application before tests...")
                stop_background()
            
            # Kill any stale test processes from previous runs
            killed = kill_test_processes()
            if killed > 0:
                print(f"Cleaned up {killed} stale test process(es)")
            
            # Run integration tests with optional filter
            test_filter = sys.argv[2] if len(sys.argv) > 2 else None
            
            test_cmd = [dotnet_path, "test", "tests/Riddle.Web.IntegrationTests/Riddle.Web.IntegrationTests.csproj", "--verbosity", "normal"]
            
            if test_filter:
                # Support filtering by test name, class name, or fully qualified name
                test_cmd.extend(["--filter", test_filter])
                print(f"Running tests matching: {test_filter}")
            else:
                print("Running all integration tests...")
            
            subprocess.run(test_cmd, check=True)
            print("Tests completed")
        
        elif command == "stop":
            stop_background()
        
        elif command == "status":
            check_status()
        
        else:
            print(f"Unknown command: {command}")
            print_usage()
            sys.exit(1)
    
    except KeyboardInterrupt:
        # Handle Ctrl+C gracefully for interactive commands
        print("\n\nShutdown requested. Exiting cleanly...")
        sys.exit(0)
    
    except subprocess.CalledProcessError:
        print(f"Error: Failed to {command} project")
        sys.exit(1)


def search_log(pattern: Optional[str] = None, tail: int = 0, level: Optional[str] = None) -> None:
    """Search or tail the riddle.log file
    
    Args:
        pattern: Regex pattern to search for (case-insensitive)
        tail: Number of lines to show from end (0 = all)
        level: Filter by log level (error, warn, info, debug)
    """
    if not LOG_FILE.exists():
        print(f"Log file not found: {LOG_FILE}")
        return
    
    try:
        with open(LOG_FILE, 'r', encoding='utf-8', errors='replace') as f:
            lines = f.readlines()
        
        # Apply tail
        if tail > 0:
            lines = lines[-tail:]
        
        # Filter by level if specified
        if level:
            level_upper = level.upper()
            level_pattern = re.compile(rf'\b{level_upper}\b|{level_upper}:', re.IGNORECASE)
            lines = [line for line in lines if level_pattern.search(line)]
        
        # Filter by pattern if specified
        if pattern:
            try:
                regex = re.compile(pattern, re.IGNORECASE)
                lines = [line for line in lines if regex.search(line)]
            except re.error as e:
                print(f"Invalid regex pattern: {e}")
                return
        
        if lines:
            for line in lines:
                print(line.rstrip())
            print(f"\n--- {len(lines)} line(s) shown ---")
        else:
            print("No matching log entries found")
    
    except Exception as e:
        print(f"Error reading log file: {e}")


def query_db(sql: str) -> None:
    """Execute a SQL query on the riddle.db database
    
    Args:
        sql: SQL query to execute
    """
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        print("Make sure the application has been run at least once.")
        return
    
    try:
        conn = sqlite3.connect(str(DB_PATH))
        conn.row_factory = sqlite3.Row
        cursor = conn.cursor()
        
        # Execute the query
        cursor.execute(sql)
        
        # Check if it's a SELECT query
        if sql.strip().upper().startswith("SELECT"):
            rows = cursor.fetchall()
            
            if rows:
                # Get column names
                columns = rows[0].keys()
                
                # Calculate column widths
                widths = {col: len(col) for col in columns}
                for row in rows:
                    for col in columns:
                        val = str(row[col]) if row[col] is not None else "NULL"
                        # Truncate long values for display
                        if len(val) > 80:
                            val = val[:77] + "..."
                        widths[col] = max(widths[col], len(val))
                
                # Print header
                header = " | ".join(col.ljust(widths[col]) for col in columns)
                print(header)
                print("-" * len(header))
                
                # Print rows
                for row in rows:
                    row_str = " | ".join(
                        (str(row[col])[:77] + "..." if len(str(row[col] or "")) > 80 else str(row[col] or "NULL")).ljust(widths[col])
                        for col in columns
                    )
                    print(row_str)
                
                print(f"\n--- {len(rows)} row(s) returned ---")
            else:
                print("Query returned no results")
        else:
            conn.commit()
            print(f"Query executed successfully. Rows affected: {cursor.rowcount}")
        
        conn.close()
    
    except sqlite3.Error as e:
        print(f"Database error: {e}")
    except Exception as e:
        print(f"Error: {e}")


def list_tables() -> None:
    """List all tables in the database"""
    query_db("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;")


def show_campaigns() -> None:
    """Show campaign instances with party state info"""
    query_db("""
        SELECT 
            Id,
            Name,
            CampaignModule,
            datetime(CreatedAt) as Created,
            length(PartyStateJson) as PartyDataLen,
            substr(PartyStateJson, 1, 100) as PartyPreview
        FROM CampaignInstances
        ORDER BY CreatedAt DESC
        LIMIT 10;
    """)


def show_characters(campaign_id: Optional[str] = None) -> None:
    """Show characters from PartyStateJson with claim status
    
    Args:
        campaign_id: Optional campaign ID to filter by. If None, shows most recent campaign.
    """
    import json
    
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        return
    
    try:
        conn = sqlite3.connect(str(DB_PATH))
        cursor = conn.cursor()
        
        if campaign_id:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances WHERE Id = ?",
                (campaign_id,)
            )
        else:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances ORDER BY CreatedAt DESC LIMIT 1"
            )
        
        row = cursor.fetchone()
        if not row:
            print("No campaigns found")
            conn.close()
            return
        
        campaign_id, campaign_name, party_json = row
        print(f"Campaign: {campaign_name}")
        print(f"ID: {campaign_id}")
        print("-" * 80)
        
        if not party_json:
            print("No party data")
            conn.close()
            return
        
        characters = json.loads(party_json)
        
        # Print header
        print(f"{'Name':<25} {'Type':<6} {'Class':<12} {'Level':<6} {'PlayerId':<40} {'PlayerName':<20}")
        print("-" * 120)
        
        for char in characters:
            name = char.get('Name', 'Unknown')[:24]
            char_type = char.get('Type', '?')[:5]
            char_class = (char.get('Class') or 'Unknown')[:11]
            level = str(char.get('Level', 1))[:5]
            player_id = (char.get('PlayerId') or '-')[:39]
            player_name = (char.get('PlayerName') or '-')[:19]
            
            print(f"{name:<25} {char_type:<6} {char_class:<12} {level:<6} {player_id:<40} {player_name:<20}")
        
        print(f"\n--- {len(characters)} character(s) ---")
        conn.close()
    
    except json.JSONDecodeError as e:
        print(f"Error parsing PartyStateJson: {e}")
    except Exception as e:
        print(f"Error: {e}")


def show_party_json(campaign_id: Optional[str] = None) -> None:
    """Show full PartyStateJson for a campaign (pretty-printed)
    
    Args:
        campaign_id: Optional campaign ID. If None, shows most recent campaign.
    """
    import json
    
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        return
    
    try:
        conn = sqlite3.connect(str(DB_PATH))
        cursor = conn.cursor()
        
        if campaign_id:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances WHERE Id = ?",
                (campaign_id,)
            )
        else:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances ORDER BY CreatedAt DESC LIMIT 1"
            )
        
        row = cursor.fetchone()
        if not row:
            print("No campaigns found")
            conn.close()
            return
        
        cid, campaign_name, party_json = row
        print(f"Campaign: {campaign_name}")
        print(f"ID: {cid}")
        print("=" * 80)
        
        if not party_json:
            print("No party data (PartyStateJson is empty)")
            conn.close()
            return
        
        # Pretty-print the JSON
        data = json.loads(party_json)
        print(json.dumps(data, indent=2))
        
        conn.close()
    
    except json.JSONDecodeError as e:
        print(f"Error parsing PartyStateJson: {e}")
        print(f"Raw JSON: {party_json}")
    except Exception as e:
        print(f"Error: {e}")


def delete_character(character_name: str, campaign_id: Optional[str] = None) -> None:
    """Delete a character from campaign's PartyStateJson
    
    Args:
        character_name: Name of the character to delete (case-insensitive)
        campaign_id: Optional campaign ID. If None, uses most recent campaign.
    """
    import json
    
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        return
    
    try:
        conn = sqlite3.connect(str(DB_PATH))
        cursor = conn.cursor()
        
        if campaign_id:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances WHERE Id = ?",
                (campaign_id,)
            )
        else:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances ORDER BY CreatedAt DESC LIMIT 1"
            )
        
        row = cursor.fetchone()
        if not row:
            print("No campaigns found")
            conn.close()
            return
        
        cid, campaign_name, party_json = row
        
        if not party_json:
            print("No characters in campaign")
            conn.close()
            return
        
        characters = json.loads(party_json)
        
        # Find and remove the character
        original_count = len(characters)
        characters = [c for c in characters if c.get('Name', '').lower() != character_name.lower()]
        
        if len(characters) == original_count:
            print(f"Character '{character_name}' not found in campaign '{campaign_name}'")
            conn.close()
            return
        
        # Save back to database
        new_json = json.dumps(characters)
        cursor.execute(
            "UPDATE CampaignInstances SET PartyStateJson = ? WHERE Id = ?",
            (new_json, cid)
        )
        conn.commit()
        conn.close()
        
        print(f"✓ Deleted character: {character_name}")
        print(f"  Campaign: {campaign_name}")
        print(f"  Remaining characters: {len(characters)}")
    
    except Exception as e:
        print(f"Error: {e}")


def create_character(json_data: str, campaign_id: Optional[str] = None) -> None:
    """Create a new character from JSON and add to campaign's PartyStateJson
    
    Args:
        json_data: JSON string or @filepath for character data
        campaign_id: Optional campaign ID. If None, uses most recent campaign.
    """
    import json
    
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        return
    
    # Handle file input
    if json_data.startswith('@'):
        filepath = Path(json_data[1:])
        if not filepath.exists():
            print(f"File not found: {filepath}")
            return
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                json_data = f.read()
        except Exception as e:
            print(f"Error reading file: {e}")
            return
    
    # Parse character JSON
    try:
        new_char = json.loads(json_data)
    except json.JSONDecodeError as e:
        print(f"Invalid JSON: {e}")
        return
    
    # Ensure required fields
    if 'Name' not in new_char:
        print("Error: Character must have a 'Name' field")
        return
    
    # Generate ID if not provided
    if 'Id' not in new_char:
        import uuid
        new_char['Id'] = str(uuid.uuid4())
    
    # Set defaults
    new_char.setdefault('Type', 'PC')
    new_char.setdefault('Level', 1)
    new_char.setdefault('Strength', 10)
    new_char.setdefault('Dexterity', 10)
    new_char.setdefault('Constitution', 10)
    new_char.setdefault('Intelligence', 10)
    new_char.setdefault('Wisdom', 10)
    new_char.setdefault('Charisma', 10)
    new_char.setdefault('MaxHp', 10)
    new_char.setdefault('CurrentHp', new_char['MaxHp'])
    new_char.setdefault('ArmorClass', 10)
    new_char.setdefault('Speed', '30 ft')
    new_char.setdefault('Languages', ['Common'])
    
    try:
        conn = sqlite3.connect(str(DB_PATH))
        cursor = conn.cursor()
        
        if campaign_id:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances WHERE Id = ?",
                (campaign_id,)
            )
        else:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances ORDER BY CreatedAt DESC LIMIT 1"
            )
        
        row = cursor.fetchone()
        if not row:
            print("No campaigns found")
            conn.close()
            return
        
        cid, campaign_name, party_json = row
        
        # Parse existing characters or start empty
        characters = json.loads(party_json) if party_json else []
        
        # Check for duplicate name
        for char in characters:
            if char.get('Name', '').lower() == new_char['Name'].lower():
                print(f"Error: Character '{new_char['Name']}' already exists in campaign")
                conn.close()
                return
        
        # Add new character
        characters.append(new_char)
        
        # Save back to database
        new_json = json.dumps(characters)
        cursor.execute(
            "UPDATE CampaignInstances SET PartyStateJson = ? WHERE Id = ?",
            (new_json, cid)
        )
        conn.commit()
        conn.close()
        
        print(f"✓ Created character: {new_char['Name']}")
        print(f"  Campaign: {campaign_name}")
        print(f"  Type: {new_char.get('Type', 'PC')}")
        print(f"  Class: {new_char.get('Class', 'Unknown')} L{new_char.get('Level', 1)}")
        print(f"  Race: {new_char.get('Race', 'Unknown')}")
        print(f"  ID: {new_char['Id']}")
    
    except Exception as e:
        print(f"Error: {e}")


def show_rolls_json(campaign_id: Optional[str] = None) -> None:
    """Show full RecentRollsJson for a campaign (pretty-printed)
    
    Args:
        campaign_id: Optional campaign ID. If None, shows most recent campaign.
    """
    import json
    
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        return
    
    try:
        conn = sqlite3.connect(str(DB_PATH))
        cursor = conn.cursor()
        
        if campaign_id:
            cursor.execute(
                "SELECT Id, Name, RecentRollsJson FROM CampaignInstances WHERE Id = ?",
                (campaign_id,)
            )
        else:
            cursor.execute(
                "SELECT Id, Name, RecentRollsJson FROM CampaignInstances ORDER BY CreatedAt DESC LIMIT 1"
            )
        
        row = cursor.fetchone()
        if not row:
            print("No campaigns found")
            conn.close()
            return
        
        cid, campaign_name, rolls_json = row
        print(f"Campaign: {campaign_name}")
        print(f"ID: {cid}")
        print("=" * 80)
        
        if not rolls_json:
            print("No roll data (RecentRollsJson is empty)")
            conn.close()
            return
        
        # Pretty-print the JSON
        data = json.loads(rolls_json)
        print(f"Total rolls: {len(data)}")
        print("-" * 80)
        
        # Display in a readable format
        for i, roll in enumerate(data):
            print(f"\n[{i+1}] {roll.get('CharacterName', 'Unknown')} - {roll.get('CheckType', '?')} = {roll.get('Result', '?')} ({roll.get('Outcome', '?')})")
            print(f"    Id: {roll.get('Id', 'N/A')}")
            print(f"    CharacterId: {roll.get('CharacterId', 'N/A')}")
            print(f"    Timestamp: {roll.get('Timestamp', 'N/A')}")
        
        conn.close()
    
    except json.JSONDecodeError as e:
        print(f"Error parsing RecentRollsJson: {e}")
        print(f"Raw JSON: {rolls_json}")
    except Exception as e:
        print(f"Error: {e}")


def clear_rolls(campaign_id: Optional[str] = None) -> None:
    """Clear all recent rolls from a campaign's RecentRollsJson
    
    Args:
        campaign_id: Optional campaign ID. If None, uses most recent campaign.
    """
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        return
    
    try:
        conn = sqlite3.connect(str(DB_PATH))
        cursor = conn.cursor()
        
        if campaign_id:
            cursor.execute(
                "SELECT Id, Name FROM CampaignInstances WHERE Id = ?",
                (campaign_id,)
            )
        else:
            cursor.execute(
                "SELECT Id, Name FROM CampaignInstances ORDER BY CreatedAt DESC LIMIT 1"
            )
        
        row = cursor.fetchone()
        if not row:
            print("No campaigns found")
            conn.close()
            return
        
        cid, campaign_name = row
        
        # Clear the rolls
        cursor.execute(
            "UPDATE CampaignInstances SET RecentRollsJson = '[]' WHERE Id = ?",
            (cid,)
        )
        conn.commit()
        conn.close()
        
        print(f"✓ Cleared recent rolls")
        print(f"  Campaign: {campaign_name}")
        print(f"  ID: {cid}")
    
    except Exception as e:
        print(f"Error: {e}")


def show_templates() -> None:
    """Show all character templates in the CharacterTemplates table"""
    query_db("""
        SELECT 
            Name,
            Race,
            Class,
            Level,
            CASE WHEN OwnerId IS NULL THEN 'System' ELSE 'User' END as Type,
            SourceFile,
            datetime(CreatedAt) as Created,
            datetime(UpdatedAt) as Updated
        FROM CharacterTemplates
        ORDER BY 
            CASE WHEN OwnerId IS NULL THEN 0 ELSE 1 END,
            Name
        LIMIT 50;
    """)


def import_templates() -> None:
    """Import all JSON files from SampleCharacters directory into CharacterTemplates table"""
    import json
    from pathlib import Path
    
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        print("Make sure the application has been run at least once to create the database.")
        return
    
    # Find SampleCharacters directory
    sample_chars_path = Path("src/Riddle.Web/Data/SampleCharacters")
    
    if not sample_chars_path.exists():
        print(f"SampleCharacters directory not found: {sample_chars_path}")
        return
    
    json_files = list(sample_chars_path.glob("*.json"))
    print(f"Found {len(json_files)} JSON files in {sample_chars_path}")
    
    if not json_files:
        return
    
    try:
        conn = sqlite3.connect(str(DB_PATH))
        cursor = conn.cursor()
        
        imported_count = 0
        updated_count = 0
        
        for filepath in json_files:
            try:
                with open(filepath, 'r', encoding='utf-8') as f:
                    json_data = f.read()
                
                character = json.loads(json_data)
                name = character.get('Name')
                
                if not name:
                    print(f"  ✗ Skipping {filepath.name}: No 'Name' field")
                    continue
                
                # Extract shadow columns for indexing
                race = character.get('Race')
                char_class = character.get('Class')
                level = character.get('Level', 1)
                source_file = filepath.name
                
                # Check if template already exists (by name, system template)
                cursor.execute(
                    "SELECT Id FROM CharacterTemplates WHERE Name = ? AND OwnerId IS NULL",
                    (name,)
                )
                existing = cursor.fetchone()
                
                now = time.strftime('%Y-%m-%d %H:%M:%S')
                
                if existing:
                    # Update existing
                    cursor.execute("""
                        UPDATE CharacterTemplates 
                        SET CharacterJson = ?, Race = ?, Class = ?, Level = ?, 
                            SourceFile = ?, UpdatedAt = ?
                        WHERE Id = ?
                    """, (json_data, race, char_class, level, source_file, now, existing[0]))
                    print(f"  ↺ Updated: {name}")
                    updated_count += 1
                else:
                    # Insert new
                    import uuid
                    template_id = str(uuid.uuid4())
                    cursor.execute("""
                        INSERT INTO CharacterTemplates 
                        (Id, Name, OwnerId, SourceFile, CharacterJson, Race, Class, Level, CreatedAt, UpdatedAt)
                        VALUES (?, ?, NULL, ?, ?, ?, ?, ?, ?, ?)
                    """, (template_id, name, source_file, json_data, race, char_class, level, now, now))
                    print(f"  ✓ Imported: {name}")
                    imported_count += 1
            
            except json.JSONDecodeError as e:
                print(f"  ✗ Failed to parse {filepath.name}: {e}")
            except Exception as e:
                print(f"  ✗ Error importing {filepath.name}: {e}")
        
        conn.commit()
        conn.close()
        
        print("-" * 40)
        print(f"Summary: {imported_count} new, {updated_count} updated")
    
    except Exception as e:
        print(f"Database error: {e}")


def backup_database(backup_name: Optional[str] = None) -> None:
    """Backup the SQLite database and WAL files
    
    Args:
        backup_name: Optional name for the backup. If None, uses timestamp.
    """
    import shutil
    
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        print("Nothing to backup.")
        return
    
    # Stop the app if running to ensure clean backup
    pid = get_running_pid()
    if pid:
        print("Stopping application for clean backup...")
        stop_background()
        time.sleep(1)  # Give it a moment to release file handles
    
    # Create backup directory
    BACKUP_DIR.mkdir(parents=True, exist_ok=True)
    
    # Generate backup name
    if not backup_name:
        backup_name = time.strftime('%Y%m%d_%H%M%S')
    
    backup_subdir = BACKUP_DIR / backup_name
    backup_subdir.mkdir(parents=True, exist_ok=True)
    
    # Copy database files
    files_backed_up = []
    db_files = [DB_PATH]
    db_files.extend(Path("src/Riddle.Web").glob("riddle.db-*"))  # WAL and SHM files
    
    for db_file in db_files:
        if db_file.exists():
            dest = backup_subdir / db_file.name
            shutil.copy2(db_file, dest)
            files_backed_up.append(db_file.name)
    
    if files_backed_up:
        print(f"✓ Database backed up to: {backup_subdir}")
        print(f"  Files: {', '.join(files_backed_up)}")
        
        # Show total size
        total_size = sum((backup_subdir / f).stat().st_size for f in files_backed_up)
        if total_size > 1024 * 1024:
            print(f"  Size: {total_size / 1024 / 1024:.2f} MB")
        elif total_size > 1024:
            print(f"  Size: {total_size / 1024:.2f} KB")
        else:
            print(f"  Size: {total_size} bytes")
    else:
        print("No database files found to backup")


def restore_database(backup_name: str) -> None:
    """Restore the SQLite database from a backup
    
    Args:
        backup_name: Name of the backup to restore from.
    """
    import shutil
    
    backup_subdir = BACKUP_DIR / backup_name
    
    if not backup_subdir.exists():
        print(f"Backup not found: {backup_subdir}")
        print("Use 'python build.py db backups' to list available backups")
        return
    
    # Check for riddle.db in backup
    backup_db = backup_subdir / "riddle.db"
    if not backup_db.exists():
        print(f"No riddle.db found in backup: {backup_subdir}")
        return
    
    # Stop the app if running
    pid = get_running_pid()
    if pid:
        print("Stopping application for restore...")
        stop_background()
        time.sleep(1)
    
    # Delete current database files
    db_files_to_delete = [DB_PATH]
    db_files_to_delete.extend(Path("src/Riddle.Web").glob("riddle.db-*"))
    
    for db_file in db_files_to_delete:
        if db_file.exists():
            db_file.unlink()
            print(f"  Removed: {db_file.name}")
    
    # Copy backup files
    files_restored = []
    for backup_file in backup_subdir.iterdir():
        if backup_file.name.startswith("riddle.db"):
            dest = Path("src/Riddle.Web") / backup_file.name
            shutil.copy2(backup_file, dest)
            files_restored.append(backup_file.name)
    
    if files_restored:
        print(f"✓ Database restored from: {backup_subdir}")
        print(f"  Files: {', '.join(files_restored)}")
    else:
        print("No database files found in backup")


def list_backups() -> None:
    """List all available database backups"""
    if not BACKUP_DIR.exists():
        print("No backups directory found")
        print("Use 'python build.py db backup' to create a backup")
        return
    
    backups = sorted(BACKUP_DIR.iterdir(), reverse=True)
    
    if not backups:
        print("No backups found")
        return
    
    print(f"{'Backup Name':<25} {'Date':<20} {'Size':<15} {'Files':<30}")
    print("-" * 90)
    
    for backup_dir in backups:
        if not backup_dir.is_dir():
            continue
        
        # Get modification time
        mtime = backup_dir.stat().st_mtime
        date_str = time.strftime('%Y-%m-%d %H:%M:%S', time.localtime(mtime))
        
        # Get files and size
        files = list(backup_dir.iterdir())
        file_names = [f.name for f in files]
        total_size = sum(f.stat().st_size for f in files)
        
        if total_size > 1024 * 1024:
            size_str = f"{total_size / 1024 / 1024:.2f} MB"
        elif total_size > 1024:
            size_str = f"{total_size / 1024:.2f} KB"
        else:
            size_str = f"{total_size} bytes"
        
        print(f"{backup_dir.name:<25} {date_str:<20} {size_str:<15} {', '.join(file_names):<30}")
    
    print(f"\n--- {len(backups)} backup(s) ---")
    print(f"Backup location: {BACKUP_DIR.absolute()}")


def delete_backup(backup_name: str) -> None:
    """Delete a database backup
    
    Args:
        backup_name: Name of the backup to delete.
    """
    import shutil
    
    backup_subdir = BACKUP_DIR / backup_name
    
    if not backup_subdir.exists():
        print(f"Backup not found: {backup_subdir}")
        return
    
    # Get info before deleting
    files = list(backup_subdir.iterdir())
    total_size = sum(f.stat().st_size for f in files)
    
    shutil.rmtree(backup_subdir)
    
    print(f"✓ Deleted backup: {backup_name}")
    print(f"  Files removed: {len(files)}")
    if total_size > 1024:
        print(f"  Space freed: {total_size / 1024:.2f} KB")


def show_character_template() -> None:
    """Print a JSON template for creating characters"""
    import json
    
    template = {
        "Name": "Character Name",
        "Type": "PC",
        "Race": "Human",
        "Class": "Fighter",
        "Level": 1,
        "Background": "Soldier",
        "Alignment": "Neutral Good",
        "Strength": 16,
        "Dexterity": 14,
        "Constitution": 14,
        "Intelligence": 10,
        "Wisdom": 12,
        "Charisma": 10,
        "ArmorClass": 16,
        "MaxHp": 12,
        "CurrentHp": 12,
        "Initiative": 2,
        "Speed": "30 ft",
        "PassivePerception": 11,
        "SavingThrowProficiencies": ["Strength", "Constitution"],
        "SkillProficiencies": ["Athletics", "Perception"],
        "ToolProficiencies": [],
        "Languages": ["Common"],
        "Weapons": ["Longsword", "Shield"],
        "Equipment": ["Chain Mail", "Explorer's Pack"],
        "GoldPieces": 10,
        "PersonalityTraits": "I'm always polite and respectful.",
        "Ideals": "Greater Good. Our lot is to lay down our lives in defense of others.",
        "Bonds": "I fight for those who cannot fight for themselves.",
        "Flaws": "I have a hard time trusting people."
    }
    
    print("# Character JSON Template")
    print("# Copy this template and modify for your character")
    print("# Usage: python build.py db create-character '<json>' OR")
    print("#        python build.py db create-character @character.json")
    print()
    print(json.dumps(template, indent=2))


def update_character(character_name: str, property_name: str, value: str, campaign_id: Optional[str] = None) -> None:
    """Update a character property in the PartyStateJson
    
    Args:
        character_name: Name of the character to update (case-insensitive match)
        property_name: Property name to update (e.g., PersonalityTraits, Ideals)
        value: New value for the property
        campaign_id: Optional campaign ID. If None, uses most recent campaign.
    """
    import json
    
    if not DB_PATH.exists():
        print(f"Database not found: {DB_PATH}")
        return
    
    try:
        conn = sqlite3.connect(str(DB_PATH))
        cursor = conn.cursor()
        
        if campaign_id:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances WHERE Id = ?",
                (campaign_id,)
            )
        else:
            cursor.execute(
                "SELECT Id, Name, PartyStateJson FROM CampaignInstances ORDER BY CreatedAt DESC LIMIT 1"
            )
        
        row = cursor.fetchone()
        if not row:
            print("No campaigns found")
            conn.close()
            return
        
        cid, campaign_name, party_json = row
        
        if not party_json:
            print("No party data in campaign")
            conn.close()
            return
        
        characters = json.loads(party_json)
        
        # Find the character (case-insensitive)
        found_char = None
        for char in characters:
            if char.get('Name', '').lower() == character_name.lower():
                found_char = char
                break
        
        if not found_char:
            print(f"Character '{character_name}' not found in campaign '{campaign_name}'")
            print("Available characters:")
            for char in characters:
                print(f"  - {char.get('Name', 'Unknown')}")
            conn.close()
            return
        
        # Get the old value for display
        old_value = found_char.get(property_name)
        
        # Handle type conversion for numeric properties
        numeric_props = ['Level', 'MaxHp', 'CurrentHp', 'TemporaryHp', 'ArmorClass', 'Initiative',
                        'PassivePerception', 'DeathSaveSuccesses', 'DeathSaveFailures',
                        'Strength', 'Dexterity', 'Constitution', 'Intelligence', 'Wisdom', 'Charisma',
                        'PlatinumPieces', 'GoldPieces', 'SilverPieces', 'CopperPieces',
                        'SpellSaveDC', 'SpellAttackBonus']
        bool_props = ['IsSpellcaster']
        
        if property_name in numeric_props:
            try:
                value = int(value)
            except ValueError:
                print(f"Error: {property_name} requires a numeric value")
                conn.close()
                return
        elif property_name in bool_props:
            value = value.lower() in ('true', '1', 'yes')
        
        # Update the character
        found_char[property_name] = value
        
        # Save back to database
        new_json = json.dumps(characters)
        cursor.execute(
            "UPDATE CampaignInstances SET PartyStateJson = ? WHERE Id = ?",
            (new_json, cid)
        )
        conn.commit()
        conn.close()
        
        print(f"✓ Updated {found_char.get('Name')}.{property_name}")
        print(f"  Campaign: {campaign_name}")
        print(f"  Old value: {old_value}")
        print(f"  New value: {value}")
    
    except json.JSONDecodeError as e:
        print(f"Error parsing PartyStateJson: {e}")
    except Exception as e:
        print(f"Error: {e}")


def print_usage() -> None:
    """Print usage information"""
    print("Usage: python build.py [command] [options]")
    print("")
    print("Build & Run Commands:")
    print("  build        - Build the project (default)")
    print("  publish      - Publish the project to ./dist")
    print("  watch        - Run with hot reload (foreground)")
    print("  run          - Run the project (foreground)")
    print("  start        - Start the project in background")
    print("  stop         - Stop the background project")
    print("  status       - Check if project is running")
    print("  test [filter]- Run integration tests (filter by test name)")
    print("")
    print("Log Commands:")
    print("  log                      - Show last 50 lines of log")
    print("  log <pattern>            - Search log for regex pattern")
    print("  log --tail <n>           - Show last n lines")
    print("  log --level <level>      - Filter by level (error/warn/info/debug)")
    print("  log --tail 100 --level error - Combine options")
    print("")
    print("Database Commands:")
    print("  db tables                - List all database tables")
    print("  db campaigns             - Show campaign instances")
    print("  db characters [id]       - Show characters from campaign")
    print("  db party [id]            - Show full PartyStateJson (pretty-printed)")
    print("  db backup [name]         - Backup database (auto-names if not specified)")
    print("  db restore <name>        - Restore database from backup")
    print("  db backups               - List all available backups")
    print("  db delete-backup <name>  - Delete a backup")
    print("  db \"<sql>\"               - Execute custom SQL query")
    print("")
    print("Examples:")
    print("  python build.py log character")
    print("  python build.py log --level error --tail 20")
    print("  python build.py db backup my_test_data")
    print("  python build.py db backups")
    print("  python build.py db restore 20260101_120000")
    print("  python build.py db \"SELECT * FROM CampaignInstances\"")
    print("  python build.py test HLR_COMBAT_010               # Filter by test name")


def main() -> None:
    """Main entry point"""
    # Parse command argument
    command = sys.argv[1] if len(sys.argv) > 1 else "build"
    
    # Special commands that don't need setup
    if command in ["stop", "status"]:
        if command == "stop":
            stop_background()
        else:
            check_status()
        return
    
    # Log command
    if command == "log":
        pattern = None
        tail = 50  # Default to last 50 lines
        level = None
        
        # Parse log options
        args = sys.argv[2:]
        i = 0
        while i < len(args):
            if args[i] == "--tail" and i + 1 < len(args):
                try:
                    tail = int(args[i + 1])
                except ValueError:
                    print(f"Invalid tail value: {args[i + 1]}")
                    sys.exit(1)
                i += 2
            elif args[i] == "--level" and i + 1 < len(args):
                level = args[i + 1]
                i += 2
            elif not args[i].startswith("--"):
                pattern = args[i]
                i += 1
            else:
                print(f"Unknown option: {args[i]}")
                print_usage()
                sys.exit(1)
        
        search_log(pattern=pattern, tail=tail, level=level)
        return
    
    # Database command
    if command == "db":
        if len(sys.argv) < 3:
            print("Usage: python build.py db <subcommand|sql>")
            print("Subcommands: tables, campaigns")
            print("Or provide a SQL query in quotes")
            sys.exit(1)
        
        subcommand = sys.argv[2]
        
        if subcommand == "tables":
            list_tables()
        elif subcommand == "campaigns":
            show_campaigns()
        elif subcommand == "characters":
            # Optional campaign ID as next argument
            campaign_id = sys.argv[3] if len(sys.argv) > 3 else None
            show_characters(campaign_id)
        elif subcommand == "party":
            # Optional campaign ID as next argument - shows full JSON
            campaign_id = sys.argv[3] if len(sys.argv) > 3 else None
            show_party_json(campaign_id)
        elif subcommand == "update":
            # Update a character property: db update <character> <property> <value>
            if len(sys.argv) < 6:
                print("Usage: python build.py db update <character_name> <property> <value>")
                print("Example: python build.py db update \"Thorin Ironforge\" PersonalityTraits \"Loves ale\"")
                sys.exit(1)
            char_name = sys.argv[3]
            prop_name = sys.argv[4]
            prop_value = sys.argv[5]
            campaign_id = sys.argv[6] if len(sys.argv) > 6 else None
            update_character(char_name, prop_name, prop_value, campaign_id)
        elif subcommand == "create-character":
            # Create a character from JSON: db create-character <json|@file> [campaign_id]
            if len(sys.argv) < 4:
                print("Usage: python build.py db create-character '<json>' [campaign_id]")
                print("       python build.py db create-character @path/to/character.json [campaign_id]")
                print("")
                print("Use 'python build.py db character-template' to see a JSON template")
                sys.exit(1)
            json_data = sys.argv[3]
            campaign_id = sys.argv[4] if len(sys.argv) > 4 else None
            create_character(json_data, campaign_id)
        elif subcommand == "character-template":
            # Show character template
            show_character_template()
        elif subcommand == "delete-character":
            # Delete a character: db delete-character <character_name> [campaign_id]
            if len(sys.argv) < 4:
                print("Usage: python build.py db delete-character <character_name> [campaign_id]")
                print("Example: python build.py db delete-character \"Test Wizard\"")
                sys.exit(1)
            char_name = sys.argv[3]
            campaign_id = sys.argv[4] if len(sys.argv) > 4 else None
            delete_character(char_name, campaign_id)
        elif subcommand == "rolls":
            # Show recent rolls: db rolls [campaign_id]
            campaign_id = sys.argv[3] if len(sys.argv) > 3 else None
            show_rolls_json(campaign_id)
        elif subcommand == "clear-rolls":
            # Clear recent rolls: db clear-rolls [campaign_id]
            campaign_id = sys.argv[3] if len(sys.argv) > 3 else None
            clear_rolls(campaign_id)
        elif subcommand == "templates":
            # Show character templates
            show_templates()
        elif subcommand == "import-templates":
            # Import JSON files from SampleCharacters into CharacterTemplates
            import_templates()
        elif subcommand == "backup":
            # Backup database: db backup [name]
            backup_name = sys.argv[3] if len(sys.argv) > 3 else None
            backup_database(backup_name)
        elif subcommand == "restore":
            # Restore database: db restore <name>
            if len(sys.argv) < 4:
                print("Usage: python build.py db restore <backup_name>")
                print("Use 'python build.py db backups' to list available backups")
                sys.exit(1)
            backup_name = sys.argv[3]
            restore_database(backup_name)
        elif subcommand == "backups":
            # List all backups
            list_backups()
        elif subcommand == "delete-backup":
            # Delete a backup: db delete-backup <name>
            if len(sys.argv) < 4:
                print("Usage: python build.py db delete-backup <backup_name>")
                sys.exit(1)
            backup_name = sys.argv[3]
            delete_backup(backup_name)
        else:
            # Treat as SQL query
            query_db(subcommand)
        return
    
    if command not in ["build", "publish", "watch", "run", "start", "test"]:
        print(f"Unknown command: {command}")
        print_usage()
        sys.exit(1)
    
    # Setup prerequisites
    print("Setting up build environment...")
    setup_tailwindcss()
    
    # Check and setup .NET
    dotnet_path = check_dotnet()
    if not dotnet_path:
        dotnet_path = install_dotnet()
    
    # Execute command
    run_dotnet_command(dotnet_path, command)


if __name__ == "__main__":
    main()
