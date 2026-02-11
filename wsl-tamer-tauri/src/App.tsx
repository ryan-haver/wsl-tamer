// WSL Tamer - Main Application

import { useState, useEffect, useMemo, useCallback } from 'react';
import { Sidebar } from './components/Sidebar';
import { ErrorBoundary } from './components/ErrorBoundary';
import { ToastProvider } from './contexts/ToastContext';
import { ConfirmProvider, useConfirm } from './contexts/ConfirmContext';
import { TextInputProvider } from './contexts/TextInputContext';
import { diskCache } from './services';
import { 
  GeneralPage, 
  DistributionsPage, 
  ProfilesPage, 
  ConfigurationPage, 
  HardwarePage, 
  AutomationPage, 
  SettingsPage, 
  AboutPage 
} from './pages';
import type { Page } from './types';
import './App.css';

// Initialize disk cache at app startup (runs scan in background)
diskCache.initDiskCache();

function AppContent() {
  const [currentPage, setCurrentPage] = useState<Page>('general');
  const [isDarkMode, setIsDarkMode] = useState(true);
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
  const confirm = useConfirm();

  // Apply dark mode class to document
  useEffect(() => {
    if (isDarkMode) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }, [isDarkMode]);

  // Handle navigation with unsaved changes check
  const handleNavigate = useCallback(async (page: Page) => {
    if (hasUnsavedChanges) {
      const confirmLeave = await confirm({
        title: 'Unsaved Changes',
        message: 'You have unsaved changes. Do you want to leave this page?\n\nYour changes will be lost if you don\'t save them.',
        confirmText: 'Leave Page',
        cancelText: 'Stay',
        danger: true
      });
      if (!confirmLeave) {
        return;
      }
      setHasUnsavedChanges(false);
    }
    setCurrentPage(page);
  }, [hasUnsavedChanges, confirm]);

  // Handle window close with unsaved changes check
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent): void => {
      if (hasUnsavedChanges) {
        e.preventDefault();
        e.returnValue = '';
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [hasUnsavedChanges]);

  const handleToggleTheme = useCallback(() => {
    setIsDarkMode(prev => !prev);
  }, []);

  const pageContent = useMemo(() => {
    switch (currentPage) {
      case 'general':
        return <GeneralPage />;
      case 'distributions':
        return <DistributionsPage />;
      case 'profiles':
        return <ProfilesPage onUnsavedChanges={setHasUnsavedChanges} />;
      case 'configuration':
        return <ConfigurationPage onUnsavedChanges={setHasUnsavedChanges} />;
      case 'hardware':
        return <HardwarePage />;
      case 'automation':
        return <AutomationPage />;
      case 'settings':
        return <SettingsPage onUnsavedChanges={setHasUnsavedChanges} />;
      case 'about':
        return <AboutPage />;
      default:
        return <GeneralPage />;
    }
  }, [currentPage]);

  return (
    <div className="app">
      <Sidebar 
        currentPage={currentPage} 
        onNavigate={handleNavigate}
        isDarkMode={isDarkMode}
        onToggleTheme={handleToggleTheme}
      />
      <main className="main-content">
        <ErrorBoundary>
          {pageContent}
        </ErrorBoundary>
      </main>
    </div>
  );
}

function App() {
  return (
    <ToastProvider>
      <ConfirmProvider>
        <TextInputProvider>
          <AppContent />
        </TextInputProvider>
      </ConfirmProvider>
    </ToastProvider>
  );
}

export default App;

