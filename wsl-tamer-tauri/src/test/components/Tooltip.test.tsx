import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { Tooltip, InfoTooltip } from '../../components/Tooltip';

describe('Tooltip', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  it('does not show tooltip content initially', () => {
    render(
      <Tooltip content="Help text">
        <button>Hover me</button>
      </Tooltip>
    );

    expect(screen.getByText('Hover me')).toBeInTheDocument();
    expect(screen.queryByText('Help text')).toBeNull();
  });

  it('shows tooltip after hover delay', () => {
    render(
      <Tooltip content="Help text">
        <button>Hover me</button>
      </Tooltip>
    );

    const wrapper = screen.getByText('Hover me').closest('.tooltip-popup-wrapper')!;

    act(() => {
      fireEvent.mouseEnter(wrapper);
      vi.advanceTimersByTime(300);
    });

    expect(screen.getByText('Help text')).toBeInTheDocument();
  });

  it('hides tooltip on mouse leave', () => {
    render(
      <Tooltip content="Help text">
        <button>Hover me</button>
      </Tooltip>
    );

    const wrapper = screen.getByText('Hover me').closest('.tooltip-popup-wrapper')!;

    act(() => {
      fireEvent.mouseEnter(wrapper);
      vi.advanceTimersByTime(300);
    });
    expect(screen.getByText('Help text')).toBeInTheDocument();

    act(() => {
      fireEvent.mouseLeave(wrapper);
    });
    expect(screen.queryByText('Help text')).toBeNull();
  });

  it('does not show tooltip if mouse leaves before delay', () => {
    render(
      <Tooltip content="Help text">
        <button>Hover me</button>
      </Tooltip>
    );

    const wrapper = screen.getByText('Hover me').closest('.tooltip-popup-wrapper')!;

    act(() => {
      fireEvent.mouseEnter(wrapper);
      vi.advanceTimersByTime(100); // Less than 300ms delay
      fireEvent.mouseLeave(wrapper);
      vi.advanceTimersByTime(300);
    });

    expect(screen.queryByText('Help text')).toBeNull();
  });

  it('applies correct position class', () => {
    render(
      <Tooltip content="Bottom tip" position="bottom">
        <button>Target</button>
      </Tooltip>
    );

    const wrapper = screen.getByText('Target').closest('.tooltip-popup-wrapper')!;

    act(() => {
      fireEvent.mouseEnter(wrapper);
      vi.advanceTimersByTime(300);
    });

    const popup = screen.getByText('Bottom tip');
    expect(popup.className).toContain('tooltip-popup-bottom');
  });

  it('renders arrow element inside tooltip', () => {
    render(
      <Tooltip content="With arrow">
        <button>Target</button>
      </Tooltip>
    );

    const wrapper = screen.getByText('Target').closest('.tooltip-popup-wrapper')!;

    act(() => {
      fireEvent.mouseEnter(wrapper);
      vi.advanceTimersByTime(300);
    });

    const popup = screen.getByText('With arrow').closest('.tooltip-popup')!;
    expect(popup.querySelector('.tooltip-popup-arrow')).toBeInTheDocument();
  });
});

describe('InfoTooltip', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  it('renders info icon', () => {
    render(<InfoTooltip content="Info text" />);
    expect(screen.getByText('ⓘ')).toBeInTheDocument();
  });

  it('shows tooltip on hover of info icon', () => {
    render(<InfoTooltip content="Info text" />);

    const wrapper = screen.getByText('ⓘ').closest('.tooltip-popup-wrapper')!;

    act(() => {
      fireEvent.mouseEnter(wrapper);
      vi.advanceTimersByTime(300);
    });

    expect(screen.getByText('Info text')).toBeInTheDocument();
  });
});
