import { API_BASE_URL } from '../../../../common/api/config'
import type { IComparisonReportRequest } from '../types/reportExport'

interface IComparisonReportPdfResponse {
  fileName: string
  pdf: Blob
}

const fallbackFileName = 'soundlens-comparison.pdf'

export const exportComparisonReportPdf = async (
  request: IComparisonReportRequest
): Promise<IComparisonReportPdfResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/report/export/comparison/pdf`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error('The comparison PDF report could not be prepared.')
  }

  return {
    fileName: parseDownloadFileName(response.headers.get('Content-Disposition')),
    pdf: await response.blob(),
  }
}

export const parseDownloadFileName = (contentDisposition: string | null): string => {
  const encodedMatch = contentDisposition?.match(/filename\*=UTF-8''([^;]+)/i)
  const plainMatch = contentDisposition?.match(/filename="?([^";]+)"?/i)
  const candidate = encodedMatch?.[1]
    ? safelyDecode(encodedMatch[1])
    : plainMatch?.[1]

  if (!candidate) {
    return fallbackFileName
  }

  const fileName = candidate.split(/[\\/]/).at(-1)?.trim()
  return fileName?.toLowerCase().endsWith('.pdf') ? fileName : fallbackFileName
}

const safelyDecode = (value: string): string => {
  try {
    return decodeURIComponent(value)
  } catch {
    return value
  }
}
