import { describe, it, expect } from 'vitest';
import { toErrorMessage } from '../../utils/errorUtils';

describe('toErrorMessage', () => {
  it('extracts message from Error instances', () => {
    expect(toErrorMessage(new Error('network failure'))).toBe('network failure');
  });

  it('extracts message from TypeError', () => {
    expect(toErrorMessage(new TypeError('bad type'))).toBe('bad type');
  });

  it('returns string errors as-is', () => {
    expect(toErrorMessage('something broke')).toBe('something broke');
  });

  it('returns empty string for empty string input', () => {
    expect(toErrorMessage('')).toBe('');
  });

  it('converts null to string', () => {
    expect(toErrorMessage(null)).toBe('null');
  });

  it('converts undefined to string', () => {
    expect(toErrorMessage(undefined)).toBe('undefined');
  });

  it('converts number to string', () => {
    expect(toErrorMessage(42)).toBe('42');
  });

  it('converts object to string', () => {
    expect(toErrorMessage({ code: 'ERR' })).toBe('[object Object]');
  });

  it('converts boolean to string', () => {
    expect(toErrorMessage(false)).toBe('false');
  });
});
