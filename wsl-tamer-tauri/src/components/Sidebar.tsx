// Sidebar component

import type { Page } from '../types';
import { APP_VERSION } from '../utils/version';

interface SidebarProps {
  currentPage: Page;
  onNavigate: (page: Page) => void;
  isDarkMode: boolean;
  onToggleTheme: () => void;
}

export function Sidebar({ currentPage, onNavigate, isDarkMode, onToggleTheme }: SidebarProps) {
  const navItems: { id: Page; label: string; icon: string }[] = [
    { id: 'general', label: 'General', icon: '⚙️' },
    { id: 'distributions', label: 'Distributions', icon: '📦' },
    { id: 'profiles', label: 'Profiles', icon: '👤' },
    { id: 'configuration', label: 'Configuration', icon: '📝' },
    { id: 'hardware', label: 'Hardware', icon: '🔌' },
    { id: 'automation', label: 'Automation', icon: '🤖' },
    { id: 'settings', label: 'Settings', icon: '🔧' },
    { id: 'about', label: 'About', icon: 'ℹ️' },
  ];

  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <h1>WSL Tamer</h1>
        <p className="version">v{APP_VERSION}</p>
      </div>

      <nav className="sidebar-nav">
        {navItems.map((item) => (
          <button
            key={item.id}
            onClick={() => onNavigate(item.id)}
            className={`nav-item ${currentPage === item.id ? 'active' : ''}`}
            aria-current={currentPage === item.id ? 'page' : undefined}
          >
            <span className="nav-icon" aria-hidden="true">{item.icon}</span>
            <span className="nav-label">{item.label}</span>
          </button>
        ))}
      </nav>

      <div className="sidebar-footer">
        <button
          onClick={onToggleTheme}
          className="nav-item theme-toggle"
          aria-label={isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'}
        >
          <span className="nav-icon" aria-hidden="true">{isDarkMode ? '☀️' : '🌙'}</span>
          <span className="nav-label">{isDarkMode ? 'Light Mode' : 'Dark Mode'}</span>
        </button>
      </div>
    </aside>
  );
}

export default Sidebar;
