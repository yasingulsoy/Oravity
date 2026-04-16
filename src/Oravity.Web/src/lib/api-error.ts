import axios from 'axios';

/** ASP.NET ProblemDetails veya benzeri gövdelerden kullanıcı mesajı çıkarır. */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data = error.response.data as Record<string, unknown>;
    const detail = data.detail;
    if (typeof detail === 'string' && detail.trim()) return detail.trim();
    const title = data.title;
    if (typeof title === 'string' && title.trim()) return title.trim();
  }
  if (error instanceof Error && error.message) return error.message;
  return fallback;
}
