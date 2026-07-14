import { API_BASE_URL } from '../../../../common/api/config'
import type { IComparisonReportRequest, IReportExportRequest } from '../types/reportExport'

interface IReportMarkdownResponse {
  fileName: string
  markdown: string
}

export const exportReportMarkdown = async (request: IReportExportRequest): Promise<IReportMarkdownResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/report/export/markdown`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error('The markdown report could not be prepared.')
  }

  return response.json() as Promise<IReportMarkdownResponse>
}

export const exportComparisonReportMarkdown = async (
  request: IComparisonReportRequest
): Promise<IReportMarkdownResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/report/export/comparison/markdown`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error('The comparison report could not be prepared.')
  }

  return response.json() as Promise<IReportMarkdownResponse>
}
