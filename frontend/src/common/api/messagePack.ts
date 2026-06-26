import { decode } from '@msgpack/msgpack'

const messagePackContentTypes = ['application/x-msgpack', 'application/msgpack']

export const isMessagePackResponse = (response: Response) => {
  const contentType = response.headers.get('content-type')?.toLowerCase() ?? ''
  return messagePackContentTypes.some((supportedType) => contentType.includes(supportedType))
}

export const readMessagePack = async <T>(response: Response): Promise<T> => {
  const arrayBuffer = await response.arrayBuffer()
  return decode(new Uint8Array(arrayBuffer)) as T
}
