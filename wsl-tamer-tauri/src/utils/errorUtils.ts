/**
 * Convert unknown error values to a human-readable message string.
 * Replaces `catch (err: any)` patterns throughout the codebase.
 */
export function toErrorMessage(err: unknown): string {
  if (err instanceof Error) return err.message;
  if (typeof err === 'string') return err;
  return String(err);
}
