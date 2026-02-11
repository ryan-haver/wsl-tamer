import { describe, it, expect } from 'vitest';
import { formatBytes } from '../../utils/formatUtils';

describe('formatBytes', () => {
  it('formats zero as "0 B"', () => {
    expect(formatBytes(0)).toBe('0 B');
  });

  it('formats negative values as "0 B"', () => {
    expect(formatBytes(-100)).toBe('0 B');
    expect(formatBytes(-1)).toBe('0 B');
  });

  it('formats NaN as "0 B"', () => {
    expect(formatBytes(NaN)).toBe('0 B');
  });

  it('formats Infinity as "0 B"', () => {
    expect(formatBytes(Infinity)).toBe('0 B');
    expect(formatBytes(-Infinity)).toBe('0 B');
  });

  it('formats bytes correctly', () => {
    expect(formatBytes(500)).toBe('500.0 B');
    expect(formatBytes(1)).toBe('1.0 B');
  });

  it('formats kilobytes correctly', () => {
    expect(formatBytes(1024)).toBe('1.0 KB');
    expect(formatBytes(1536)).toBe('1.5 KB');
  });

  it('formats megabytes correctly', () => {
    expect(formatBytes(1048576)).toBe('1.0 MB');
    expect(formatBytes(1572864)).toBe('1.5 MB');
  });

  it('formats gigabytes correctly', () => {
    expect(formatBytes(1073741824)).toBe('1.0 GB');
  });

  it('formats terabytes correctly', () => {
    expect(formatBytes(1099511627776)).toBe('1.0 TB');
  });
});
