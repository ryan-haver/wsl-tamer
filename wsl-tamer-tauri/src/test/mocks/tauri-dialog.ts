// Mock Tauri dialog plugin for testing
import { vi } from 'vitest';

export const open = vi.fn(async () => '/mock/path/to/file.tar');
export const save = vi.fn(async () => '/mock/path/to/save.tar');
export const message = vi.fn(async () => undefined);
export const ask = vi.fn(async () => true);
export const confirm = vi.fn(async () => true);

export default { open, save, message, ask, confirm };
