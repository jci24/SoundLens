import { decode } from '@msgpack/msgpack'

const messagePackContentTypes = ['application/x-msgpack', 'application/msgpack']

export const isMessagePackResponse = (response: Response) => {
  const contentType = response.headers.get('content-type')?.toLowerCase() ?? ''
  return messagePackContentTypes.some((supportedType) => contentType.includes(supportedType))
}

export const readMessagePack = async <T>(response: Response): Promise<T> => {
  const arrayBuffer = await response.arrayBuffer()
  return normalizeMessagePackValue(decode(new Uint8Array(arrayBuffer))) as T
}

const normalizeMessagePackValue = (value: unknown): unknown => {
  if (Array.isArray(value)) {
    return value.map(normalizeMessagePackValue)
  }

  if (!value || typeof value !== 'object') {
    return value
  }

  return Object.fromEntries(
    Object.entries(value).map(([key, nestedValue]) => [toCamelCase(key), normalizeMessagePackValue(nestedValue)])
  )
}

const toCamelCase = (value: string) => {
  if (value.length === 0) {
    return value
  }

  return `${value[0]!.toLowerCase()}${value.slice(1)}`
}
