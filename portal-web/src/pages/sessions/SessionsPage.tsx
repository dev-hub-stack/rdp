import React, { useState, useEffect } from 'react'
import {
  Box,
  Typography,
  Button,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  MenuItem,
  Alert,
  Menu,
  Tooltip,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper
} from '@mui/material'
import {
  Add,
  MoreVert,
  PlayArrow,
  Stop,
  Refresh,
  ContentCopy,
  Computer
} from '@mui/icons-material'
import { agentsApi } from '../../services/agentsApi'
import { sessionsApi } from '../../services/sessionsApi'
import { Agent, Session, CreateSessionRequest } from '../../types'

const SessionsPage: React.FC = () => {
  const [sessions, setSessions] = useState<Session[]>([])
  const [agents, setAgents] = useState<Agent[]>([])
  const [loading, setLoading] = useState(true)
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [connectDialogOpen, setConnectDialogOpen] = useState(false)
  const [selectedAgentId, setSelectedAgentId] = useState('')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [connectCode, setConnectCode] = useState('')
  const [connectInfo, setConnectInfo] = useState<{ host: string; port: number } | null>(null)
  const [error, setError] = useState('')
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [selectedSession, setSelectedSession] = useState<Session | null>(null)

  const fetchData = async () => {
    try {
      setLoading(true)
      const [sessionsResponse, agentsResponse] = await Promise.all([
        sessionsApi.getSessions(1, 100),
        agentsApi.getAgents(1, 100)
      ])
      setSessions(sessionsResponse.items)
      // Handle both string and numeric status values - 1 = Online, 'Online' = Online (case-insensitive)
      setAgents(agentsResponse.items.filter(agent => 
        agent.status === 'Online' || agent.status === 'online' || agent.status === '1'
      ))
    } catch (error) {
      console.error('Failed to fetch data:', error)
      setError('Failed to fetch data')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchData()
  }, [])

  const handleCreateSession = async () => {
    if (!selectedAgentId || !username.trim()) {
      setError('Agent and username are required')
      return
    }

    try {
      const newSession: CreateSessionRequest = {
        agentId: selectedAgentId,
        username: username.trim()
      }

      const session = await sessionsApi.createSession(newSession)
      setConnectCode(session.connectCode)
      setConnectDialogOpen(true)
      setCreateDialogOpen(false)
      setSelectedAgentId('')
      setUsername('')
      setPassword('')
      setError('')
      fetchData()
    } catch (error: any) {
      setError(error.response?.data?.message || 'Failed to create session')
    }
  }

  const handleGetConnectInfo = async () => {
    if (!connectCode) return

    try {
      const info = await sessionsApi.getConnectInfo(connectCode)
      setConnectInfo(info)
    } catch (error: any) {
      setError(error.response?.data?.message || 'Failed to get connection info')
    }
  }

  const handleTerminateSession = async (session: Session) => {
    if (window.confirm(`Are you sure you want to terminate session "${session.id}"?`)) {
      try {
        await sessionsApi.terminateSession(session.id)
        fetchData()
        handleMenuClose()
      } catch (error: any) {
        setError(error.response?.data?.message || 'Failed to terminate session')
      }
    }
  }

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, session: Session) => {
    setAnchorEl(event.currentTarget)
    setSelectedSession(session)
  }

  const handleMenuClose = () => {
    setAnchorEl(null)
    setSelectedSession(null)
  }

  const copyToClipboard = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text)
    } catch (error) {
      console.error('Failed to copy to clipboard:', error)
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return 'success'
      case 'connecting':
        return 'warning'
      case 'disconnected':
        return 'default'
      case 'error':
        return 'error'
      default:
        return 'default'
    }
  }

  const formatDuration = (startTime: Date, endTime?: Date) => {
    const start = new Date(startTime)
    const end = endTime ? new Date(endTime) : new Date()
    const duration = Math.floor((end.getTime() - start.getTime()) / 1000)
    
    const hours = Math.floor(duration / 3600)
    const minutes = Math.floor((duration % 3600) / 60)
    const seconds = duration % 60
    
    return `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight="bold">
          Sessions
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="contained"
            startIcon={<Add />}
            onClick={() => setCreateDialogOpen(true)}
            disabled={agents.length === 0}
          >
            New Session
          </Button>
          <Tooltip title="Refresh">
            <IconButton onClick={fetchData} disabled={loading}>
              <Refresh />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
          {error}
        </Alert>
      )}

      {agents.length === 0 && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          No online agents available. Make sure at least one agent is running and connected.
        </Alert>
      )}

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : sessions.length === 0 ? (
        <Box sx={{ textAlign: 'center', py: 4 }}>
          <PlayArrow sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
          <Typography variant="h6" color="text.secondary" gutterBottom>
            No sessions found
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Create your first RDP session to get started
          </Typography>
          {agents.length > 0 && (
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setCreateDialogOpen(true)}
            >
              New Session
            </Button>
          )}
        </Box>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Agent</TableCell>
                <TableCell>User</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Connect Code</TableCell>
                <TableCell>Duration</TableCell>
                <TableCell>Created</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {sessions.map((session) => (
                <TableRow key={session.id}>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Computer fontSize="small" />
                      {session.agentName}
                    </Box>
                  </TableCell>
                  <TableCell>{session.username}</TableCell>
                  <TableCell>
                    <Chip
                      label={session.status}
                      color={getStatusColor(session.status) as any}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="body2" fontFamily="monospace">
                        {session.connectCode}
                      </Typography>
                      <IconButton size="small" onClick={() => copyToClipboard(session.connectCode)}>
                        <ContentCopy fontSize="small" />
                      </IconButton>
                    </Box>
                  </TableCell>
                  <TableCell>
                    {formatDuration(new Date(session.createdAt), session.endedAt ? new Date(session.endedAt) : undefined)}
                  </TableCell>
                  <TableCell>
                    {new Date(session.createdAt).toLocaleString()}
                  </TableCell>
                  <TableCell>
                    <IconButton
                      size="small"
                      onClick={(e) => handleMenuClick(e, session)}
                    >
                      <MoreVert />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Actions Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        {selectedSession?.status === 'active' && (
          <MenuItem onClick={() => selectedSession && handleTerminateSession(selectedSession)}>
            <Stop sx={{ mr: 1 }} />
            Terminate
          </MenuItem>
        )}
        <MenuItem onClick={() => selectedSession && copyToClipboard(selectedSession.connectCode)}>
          <ContentCopy sx={{ mr: 1 }} />
          Copy Connect Code
        </MenuItem>
      </Menu>

      {/* Create Session Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New Session</DialogTitle>
        <DialogContent>
          <TextField
            select
            margin="dense"
            label="Agent"
            fullWidth
            variant="outlined"
            value={selectedAgentId}
            onChange={(e) => setSelectedAgentId(e.target.value)}
            sx={{ mb: 2 }}
          >
            {agents.map((agent) => (
              <MenuItem key={agent.id} value={agent.id}>
                {agent.name} ({agent.hostname})
              </MenuItem>
            ))}
          </TextField>
          <TextField
            margin="dense"
            label="Username"
            fullWidth
            variant="outlined"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Password (Optional)"
            type="password"
            fullWidth
            variant="outlined"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleCreateSession} variant="contained">
            Create Session
          </Button>
        </DialogActions>
      </Dialog>

      {/* Connect Info Dialog */}
      <Dialog open={connectDialogOpen} onClose={() => setConnectDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Session Created Successfully</DialogTitle>
        <DialogContent>
          <Alert severity="success" sx={{ mb: 2 }}>
            Your RDP session has been created. Use the connection details below to connect.
          </Alert>
          
          <TextField
            label="Connect Code"
            fullWidth
            value={connectCode}
            InputProps={{
              readOnly: true,
              endAdornment: (
                <IconButton onClick={() => copyToClipboard(connectCode)}>
                  <ContentCopy />
                </IconButton>
              )
            }}
            sx={{ mb: 2 }}
          />

          <Button
            variant="outlined"
            onClick={handleGetConnectInfo}
            sx={{ mb: 2 }}
          >
            Get Connection Details
          </Button>

          {connectInfo && (
            <Box>
              <Typography variant="h6" gutterBottom>
                Connection Details:
              </Typography>
              <TextField
                label="Host"
                value={connectInfo.host}
                InputProps={{ readOnly: true }}
                sx={{ mb: 1, mr: 1 }}
              />
              <TextField
                label="Port"
                value={connectInfo.port}
                InputProps={{ readOnly: true }}
                sx={{ mb: 1 }}
              />
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => copyToClipboard(connectCode)} startIcon={<ContentCopy />}>
            Copy Connect Code
          </Button>
          <Button onClick={() => setConnectDialogOpen(false)} variant="contained">
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

export default SessionsPage
