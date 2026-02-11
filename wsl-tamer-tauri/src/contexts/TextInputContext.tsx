// Text Input Dialog Context - Replace browser prompt()

import { useState, useCallback, createContext, useContext, useRef, useEffect, ReactNode } from 'react';

interface TextInputOptions {
  title: string;
  message: string;
  placeholder?: string;
  defaultValue?: string;
  confirmText?: string;
  cancelText?: string;
}

interface TextInputContextValue {
  textInput: (options: TextInputOptions) => Promise<string | null>;
}

const TextInputContext = createContext<TextInputContextValue | null>(null);

export function TextInputProvider({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState(false);
  const [options, setOptions] = useState<TextInputOptions | null>(null);
  const [value, setValue] = useState('');
  const [resolveRef, setResolveRef] = useState<((value: string | null) => void) | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus();
      inputRef.current.select();
    }
  }, [isOpen]);

  const textInput = useCallback((opts: TextInputOptions): Promise<string | null> => {
    setOptions(opts);
    setValue(opts.defaultValue || '');
    setIsOpen(true);

    return new Promise<string | null>((resolve) => {
      setResolveRef(() => resolve);
    });
  }, []);

  const handleConfirm = () => {
    const trimmed = value.trim();
    setIsOpen(false);
    resolveRef?.(trimmed || null);
  };

  const handleCancel = () => {
    setIsOpen(false);
    resolveRef?.(null);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleConfirm();
    if (e.key === 'Escape') handleCancel();
  };

  return (
    <TextInputContext.Provider value={{ textInput }}>
      {children}
      {isOpen && options && (
        <div className="modal-overlay" onClick={handleCancel} role="dialog" aria-modal="true" aria-labelledby="text-input-title">
          <div className="confirm-dialog" onClick={e => e.stopPropagation()}>
            <h3 className="confirm-title" id="text-input-title">{options.title}</h3>
            <p className="confirm-message">{options.message}</p>
            <input
              ref={inputRef}
              type="text"
              className="form-input"
              value={value}
              onChange={e => setValue(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={options.placeholder || ''}
            />
            <div className="confirm-actions">
              <button
                className="btn btn-secondary"
                onClick={handleCancel}
              >
                {options.cancelText || 'Cancel'}
              </button>
              <button
                className="btn btn-primary"
                onClick={handleConfirm}
                disabled={!value.trim()}
              >
                {options.confirmText || 'OK'}
              </button>
            </div>
          </div>
        </div>
      )}
    </TextInputContext.Provider>
  );
}

export function useTextInput() {
  const context = useContext(TextInputContext);
  if (!context) {
    throw new Error('useTextInput must be used within a TextInputProvider');
  }
  return context.textInput;
}

export default TextInputProvider;
