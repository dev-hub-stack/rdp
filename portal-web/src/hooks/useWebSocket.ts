import { useEffect, useRef, useState } from 'react'

interface UseWebSocketOptions {
  url: string
  onMessage?: (event: MessageEvent) => void
  onOpen?: (event: Event) => void
  onClose?: (event: CloseEvent) => void
  onError?: (event: Event) => void
  autoReconnect?: boolean
  reconnectInterval?: number
}

export const useWebSocket = (options: UseWebSocketOptions) => {
  const {
    url,
    onMessage,
    onOpen,
    onClose,
    onError,
    autoReconnect = true,
    reconnectInterval = 3000
  } = options

  const [readyState, setReadyState] = useState<number>(WebSocket.CONNECTING)
  const [lastMessage, setLastMessage] = useState<MessageEvent | null>(null)
  const websocketRef = useRef<WebSocket | null>(null)
  const reconnectTimeoutRef = useRef<number | null>(null)

  const connect = () => {
    try {
      const ws = new WebSocket(url)
      websocketRef.current = ws

      ws.onopen = (event) => {
        setReadyState(WebSocket.OPEN)
        onOpen?.(event)
      }

      ws.onmessage = (event) => {
        setLastMessage(event)
        onMessage?.(event)
      }

      ws.onclose = (event) => {
        setReadyState(WebSocket.CLOSED)
        onClose?.(event)

        if (autoReconnect && !event.wasClean) {
          reconnectTimeoutRef.current = setTimeout(() => {
            connect()
          }, reconnectInterval) as unknown as number
        }
      }

      ws.onerror = (event) => {
        setReadyState(WebSocket.CLOSED)
        onError?.(event)
      }
    } catch (error) {
      console.error('WebSocket connection failed:', error)
      setReadyState(WebSocket.CLOSED)
    }
  }

  const disconnect = () => {
    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current as unknown as NodeJS.Timeout)
      reconnectTimeoutRef.current = null
    }

    if (websocketRef.current) {
      websocketRef.current.close()
      websocketRef.current = null
    }
  }

  const sendMessage = (message: string | object) => {
    if (websocketRef.current?.readyState === WebSocket.OPEN) {
      const data = typeof message === 'string' ? message : JSON.stringify(message)
      websocketRef.current.send(data)
      return true
    }
    return false
  }

  useEffect(() => {
    connect()
    return disconnect
  }, [url])

  return {
    readyState,
    lastMessage,
    sendMessage,
    connect,
    disconnect
  }
}
