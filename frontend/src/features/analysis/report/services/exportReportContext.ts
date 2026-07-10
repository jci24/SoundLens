import { API_BASE_URL } from '../../../../common/api/config'
import type { IReportExportRequest, IReportExportResponse } from '../types/reportExport'

export const exportReportContext = async (request: IReportExportRequest): Promise<IReportExportResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/report/export`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error('The report context could not be prepared.')
  }

  return response.json() as Promise<IReportExportResponse>
}
