import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { Sidebar } from '../../components/Sidebar';
import type { Page } from '../../types';

describe('Sidebar', () => {
  const defaultProps = {
    currentPage: 'general' as Page,
    onNavigate: vi.fn(),
    isDarkMode: false,
    onToggleTheme: vi.fn(),
  };

  it('renders all navigation items', () => {
    render(<Sidebar {...defaultProps} />);

    expect(screen.getByText('General')).toBeInTheDocument();
    expect(screen.getByText('Distributions')).toBeInTheDocument();
    expect(screen.getByText('Profiles')).toBeInTheDocument();
    expect(screen.getByText('Configuration')).toBeInTheDocument();
    expect(screen.getByText('Hardware')).toBeInTheDocument();
    expect(screen.getByText('Automation')).toBeInTheDocument();
    expect(screen.getByText('Settings')).toBeInTheDocument();
    expect(screen.getByText('About')).toBeInTheDocument();
  });

  it('renders app title and version', () => {
    render(<Sidebar {...defaultProps} />);

    expect(screen.getByText('WSL Tamer')).toBeInTheDocument();
    expect(screen.getByText(/v\d+\.\d+\.\d+/)).toBeInTheDocument();
  });

  it('highlights the active nav item', () => {
    render(<Sidebar {...defaultProps} currentPage="distributions" />);

    const activeButton = screen.getByText('Distributions').closest('button');
    expect(activeButton?.className).toContain('active');

    const inactiveButton = screen.getByText('General').closest('button');
    expect(inactiveButton?.className).not.toContain('active');
  });

  it('calls onNavigate when a nav item is clicked', () => {
    const onNavigate = vi.fn();
    render(<Sidebar {...defaultProps} onNavigate={onNavigate} />);

    fireEvent.click(screen.getByText('Profiles'));
    expect(onNavigate).toHaveBeenCalledWith('profiles');
  });

  it('shows dark mode toggle text when in light mode', () => {
    render(<Sidebar {...defaultProps} isDarkMode={false} />);
    expect(screen.getByText('Dark Mode')).toBeInTheDocument();
  });

  it('shows light mode toggle text when in dark mode', () => {
    render(<Sidebar {...defaultProps} isDarkMode={true} />);
    expect(screen.getByText('Light Mode')).toBeInTheDocument();
  });

  it('calls onToggleTheme when theme button is clicked', () => {
    const onToggleTheme = vi.fn();
    render(<Sidebar {...defaultProps} onToggleTheme={onToggleTheme} />);

    fireEvent.click(screen.getByText('Dark Mode'));
    expect(onToggleTheme).toHaveBeenCalledOnce();
  });
});
