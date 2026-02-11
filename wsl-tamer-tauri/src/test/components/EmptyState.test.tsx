import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { EmptyState } from '../../components/EmptyState';

describe('EmptyState', () => {
  it('renders title and default icon', () => {
    render(<EmptyState title="No items found" />);

    expect(screen.getByText('No items found')).toBeInTheDocument();
    expect(screen.getByText('📭')).toBeInTheDocument();
  });

  it('renders custom icon', () => {
    render(<EmptyState title="Test" icon="🎉" />);
    expect(screen.getByText('🎉')).toBeInTheDocument();
  });

  it('renders description when provided', () => {
    render(
      <EmptyState title="Empty" description="Try adding some items" />
    );
    expect(screen.getByText('Try adding some items')).toBeInTheDocument();
  });

  it('omits description element when not provided', () => {
    const { container } = render(<EmptyState title="Empty" />);
    expect(container.querySelector('.empty-state-description')).toBeNull();
  });

  it('renders action button and calls onClick', () => {
    const onClick = vi.fn();
    render(
      <EmptyState
        title="Empty"
        action={{ label: 'Add Item', onClick }}
      />
    );

    const btn = screen.getByText('Add Item');
    expect(btn).toBeInTheDocument();
    expect(btn.tagName).toBe('BUTTON');

    fireEvent.click(btn);
    expect(onClick).toHaveBeenCalledOnce();
  });

  it('omits action button when not provided', () => {
    const { container } = render(<EmptyState title="Empty" />);
    expect(container.querySelector('.empty-state-action')).toBeNull();
  });

  it('renders children alongside standard content', () => {
    render(
      <EmptyState title="Empty">
        <span data-testid="child">Custom child content</span>
      </EmptyState>
    );
    expect(screen.getByTestId('child')).toBeInTheDocument();
  });
});
