import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useDirtyCheck } from '../../hooks/useDirtyCheck';

describe('useDirtyCheck', () => {
  it('starts clean (not dirty)', () => {
    const { result } = renderHook(() => useDirtyCheck({ name: 'Alice' }));
    expect(result.current.isDirty).toBe(false);
  });

  it('initial state matches the provided value', () => {
    const { result } = renderHook(() => useDirtyCheck({ count: 42 }));
    expect(result.current.state).toEqual({ count: 42 });
  });

  it('becomes dirty after setState', () => {
    const { result } = renderHook(() => useDirtyCheck({ name: 'Alice' }));

    act(() => {
      result.current.setState({ name: 'Bob' });
    });

    expect(result.current.isDirty).toBe(true);
    expect(result.current.state).toEqual({ name: 'Bob' });
  });

  it('returns clean if setState restores original value', () => {
    const { result } = renderHook(() => useDirtyCheck({ name: 'Alice' }));

    act(() => {
      result.current.setState({ name: 'Bob' });
    });
    expect(result.current.isDirty).toBe(true);

    act(() => {
      result.current.setState({ name: 'Alice' });
    });
    expect(result.current.isDirty).toBe(false);
  });

  it('reset without argument makes current state the new baseline', () => {
    const { result } = renderHook(() => useDirtyCheck({ name: 'Alice' }));

    act(() => {
      result.current.setState({ name: 'Bob' });
    });
    expect(result.current.isDirty).toBe(true);

    // reset() sets baseline ref to current state; isDirty recalculates on next render
    act(() => {
      result.current.reset();
      // Force re-render by setting state to same value
      result.current.setState({ name: 'Bob' });
    });
    expect(result.current.isDirty).toBe(false);
    expect(result.current.state).toEqual({ name: 'Bob' });
  });

  it('reset with argument sets both state and baseline', () => {
    const { result } = renderHook(() => useDirtyCheck({ name: 'Alice' }));

    act(() => {
      result.current.setState({ name: 'Bob' });
    });

    act(() => {
      result.current.reset({ name: 'Charlie' });
    });

    expect(result.current.isDirty).toBe(false);
    expect(result.current.state).toEqual({ name: 'Charlie' });
  });

  it('supports functional updates', () => {
    const { result } = renderHook(() => useDirtyCheck(0));

    act(() => {
      result.current.setState((prev: number) => prev + 1);
    });

    expect(result.current.state).toBe(1);
    expect(result.current.isDirty).toBe(true);
  });

  it('works with primitive values', () => {
    const { result } = renderHook(() => useDirtyCheck('hello'));

    expect(result.current.isDirty).toBe(false);

    act(() => {
      result.current.setState('world');
    });
    expect(result.current.isDirty).toBe(true);

    act(() => {
      result.current.setState('hello');
    });
    expect(result.current.isDirty).toBe(false);
  });
});
