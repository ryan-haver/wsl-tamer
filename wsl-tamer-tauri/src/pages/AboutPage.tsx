// About Page

import { APP_VERSION } from '../utils/version';

export function AboutPage() {
  return (
    <div className="page">
      <header className="page-header">
        <h1>About WSL Tamer</h1>
        <p>Advanced WSL2 Distribution Manager</p>
      </header>

      <section className="card about-card">
        <div className="about-logo">🐧</div>
        <h2>WSL Tamer v{APP_VERSION}</h2>
        <p className="about-tagline">Tame your Windows Subsystem for Linux</p>
        
        <div className="about-features">
          <h3>Features</h3>
          <ul>
            <li>📦 Distribution Management - Import, export, clone, move</li>
            <li>👤 Resource Profiles - Eco, Balanced, Unleashed modes</li>
            <li>📝 Configuration - Per-distro wsl.conf editing</li>
            <li>🔌 Hardware Passthrough - USB, physical disks</li>
            <li>🤖 Automation - Time, process, and power triggers</li>
            <li>🖥️ System Tray - Quick access and status monitoring</li>
          </ul>
        </div>

        <div className="about-tech">
          <h3>Built With</h3>
          <div className="tech-stack">
            <span className="tech-badge">Tauri 2.0</span>
            <span className="tech-badge">Rust</span>
            <span className="tech-badge">React</span>
            <span className="tech-badge">TypeScript</span>
          </div>
        </div>

        <div className="about-links">
          <a 
            href="https://github.com/ryan-haver/wsl-tamer" 
            target="_blank" 
            rel="noopener noreferrer"
            className="btn btn-secondary"
          >
            📂 GitHub Repository
          </a>
          <a 
            href="https://github.com/ryan-haver/wsl-tamer/issues" 
            target="_blank" 
            rel="noopener noreferrer"
            className="btn btn-secondary"
          >
            🐛 Report Issue
          </a>
        </div>

        <div className="about-footer">
          <p>© 2025-2026 Ryan Haver. MIT License.</p>
        </div>
      </section>
    </div>
  );
}

export default AboutPage;
