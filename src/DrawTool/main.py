"""
MapDrawer Application Entry Point
"""

import sys
import os

# Add the parent directory to Python path for imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from MapDrawer.ui.main_window import MapDrawer


class MapDrawerApp:
    """Main application class"""
    
    def __init__(self):
        self.app = MapDrawer()
    
    def run(self):
        """Run the application"""
        self.app.run()


def main():
    """Main entry point"""
    app = MapDrawerApp()
    app.run()


if __name__ == "__main__":
    app = MapDrawer()
    app.run()

