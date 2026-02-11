// useDirtyCheck - Track unsaved state against a baseline
// Replaces manual hasChanges patterns across pages

import { useState, useCallback, useRef } from 'react';

export function useDirtyCheck<T>(initial: T) {
  const baseline = useRef<string>(JSON.stringify(initial));
  const [state, setStateInternal] = useState<T>(initial);

  const isDirty = JSON.stringify(state) !== baseline.current;

  const setState = useCallback((val: T | ((prev: T) => T)) => {
    setStateInternal(val);
  }, []);

  const reset = useCallback((newBaseline?: T) => {
    if (newBaseline !== undefined) {
      baseline.current = JSON.stringify(newBaseline);
      setStateInternal(newBaseline);
    } else {
      baseline.current = JSON.stringify(state);
    }
  }, [state]);

  return { state, setState, isDirty, reset };
}
