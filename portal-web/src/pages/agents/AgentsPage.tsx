import React, { useState, useEffect } from 'react'
import {
  Box,
  Typography,
  Button,
  Card,
  CardContent,
  Grid,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Alert,
  Menu,
  MenuItem,
  Tooltip,
  CircularProgress
} from '@mui/material'
import {
  Add,
  MoreVert,
  Computer,
  Refresh,
  Delete,
  ContentCopy,
  PowerSettingsNew
} from '@mui/icons-material'
import { agentsApi } from '../../services/agentsApi'
import { Agent, CreateAgentRequest } from '../../types'

const AgentsPage: React.FC = () => {
  const [agents, setAgents] = useState<Agent[]>([])
  const [loading, setLoading] = useState(true)
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [provisioningDialogOpen, setProvisioningDialogOpen] = useState(false)
  const [provisioningToken, setProvisioningToken] = useState('')
  const [newAgentName, setNewAgentName] = useState('')
  const [newAgentDescription, setNewAgentDescription] = useState('')
  const [newAgentMachineId, setNewAgentMachineId] = useState('')
  const [newAgentMachineName, setNewAgentMachineName] = useState('')
  const [newAgentIpAddress, setNewAgentIpAddress] = useState('')
  const [error, setError] = useState('')
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [selectedAgent, setSelectedAgent] = useState<Agent | null>(null)

  const fetchAgents = async () => {
    try {
      setLoading(true)
      const response = await agentsApi.getAgents(1, 100)
      setAgents(response.items)
    } catch (error) {
      console.error('Failed to fetch agents:', error)
      setError('Failed to fetch agents')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchAgents()
  }, [])

  const handleCreateAgent = async () => {
    if (!newAgentName.trim()) {
      setError('Agent name is required')
      return
    }

    if (!newAgentMachineId.trim()) {
      setError('Machine ID is required')
      return
    }

    try {
      const newAgent: CreateAgentRequest = {
        name: newAgentName.trim(),
        description: newAgentDescription.trim() || undefined,
        machineId: newAgentMachineId.trim(),
        machineName: newAgentMachineName.trim() || undefined,
        ipAddress: newAgentIpAddress.trim() || undefined
      }

      await agentsApi.createAgent(newAgent)
      setCreateDialogOpen(false)
      setNewAgentName('')
      setNewAgentDescription('')
      setNewAgentMachineId('')
      setNewAgentMachineName('')
      setNewAgentIpAddress('')
      setError('')
      fetchAgents()
    } catch (error: any) {
      setError(error.response?.data?.message || 'Failed to create agent')
    }
  }

  const handleGenerateToken = async () => {
    try {
      const response = await agentsApi.generateProvisioningToken()
      setProvisioningToken(response.token)
      setProvisioningDialogOpen(true)
    } catch (error: any) {
      setError(error.response?.data?.message || 'Failed to generate token')
    }
  }

  const handleDeleteAgent = async (agent: Agent) => {
    if (window.confirm(`Are you sure you want to delete agent "${agent.name}"?`)) {
      try {
        await agentsApi.deleteAgent(agent.id)
        fetchAgents()
        handleMenuClose()
      } catch (error: any) {
        setError(error.response?.data?.message || 'Failed to delete agent')
      }
    }
  }

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, agent: Agent) => {
    setAnchorEl(event.currentTarget)
    setSelectedAgent(agent)
  }

  const handleMenuClose = () => {
    setAnchorEl(null)
    setSelectedAgent(null)
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
      case 'online':
        return 'success'
      case 'offline':
        return 'default'
      case 'error':
        return 'error'
      default:
        return 'default'
    }
  }

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'online':
        return <PowerSettingsNew sx={{ color: 'success.main' }} />
      case 'offline':
        return <PowerSettingsNew sx={{ color: 'text.secondary' }} />
      case 'error':
        return <PowerSettingsNew sx={{ color: 'error.main' }} />
      default:
        return <PowerSettingsNew sx={{ color: 'text.secondary' }} />
    }
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight="bold">
          Agents
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<ContentCopy />}
            onClick={handleGenerateToken}
          >
            Generate Token
          </Button>
          <Button
            variant="contained"
            startIcon={<Add />}
            onClick={() => setCreateDialogOpen(true)}
          >
            Add Agent
          </Button>
          <Tooltip title="Refresh">
            <IconButton onClick={fetchAgents} disabled={loading}>
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

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {agents.map((agent) => (
            <Grid item xs={12} sm={6} md={4} key={agent.id}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Computer />
                      <Typography variant="h6" noWrap>
                        {agent.name}
                      </Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      {getStatusIcon(agent.status)}
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuClick(e, agent)}
                      >
                        <MoreVert />
                      </IconButton>
                    </Box>
                  </Box>

                  <Box sx={{ mb: 2 }}>
                    <Chip
                      label={agent.status}
                      color={getStatusColor(agent.status) as any}
                      size="small"
                      sx={{ mb: 1 }}
                    />
                  </Box>

                  <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                    <strong>Hostname:</strong> {agent.hostname || 'N/A'}
                  </Typography>
                  
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                    <strong>OS:</strong> {agent.operatingSystem || 'N/A'}
                  </Typography>

                  <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                    <strong>Version:</strong> {agent.version || 'N/A'}
                  </Typography>

                  <Typography variant="body2" color="text.secondary">
                    <strong>Last Seen:</strong>{' '}
                    {agent.lastSeen ? new Date(agent.lastSeen).toLocaleString() : 'Never'}
                  </Typography>

                  {agent.description && (
                    <Typography variant="body2" sx={{ mt: 2, fontStyle: 'italic' }}>
                      {agent.description}
                    </Typography>
                  )}
                </CardContent>
              </Card>
            </Grid>
          ))}
          
          {agents.length === 0 && (
            <Grid item xs={12}>
              <Box sx={{ textAlign: 'center', py: 4 }}>
                <Computer sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" color="text.secondary" gutterBottom>
                  No agents registered
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  Add your first agent to get started
                </Typography>
                <Button
                  variant="contained"
                  startIcon={<Add />}
                  onClick={() => setCreateDialogOpen(true)}
                >
                  Add Agent
                </Button>
              </Box>
            </Grid>
          )}
        </Grid>
      )}

      {/* Actions Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem onClick={() => selectedAgent && handleDeleteAgent(selectedAgent)}>
          <Delete sx={{ mr: 1 }} />
          Delete
        </MenuItem>
      </Menu>

      {/* Create Agent Dialog */}
      <Dialog open={createDialogOpen} onClose={() => {
        setCreateDialogOpen(false)
        setNewAgentName('')
        setNewAgentDescription('')
        setNewAgentMachineId('')
        setNewAgentMachineName('')
        setNewAgentIpAddress('')
        setError('')
      }} maxWidth="sm" fullWidth>
        <DialogTitle>Add New Agent</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Agent Name"
            fullWidth
            variant="outlined"
            value={newAgentName}
            onChange={(e) => setNewAgentName(e.target.value)}
            sx={{ mb: 2 }}
            required
          />
          <TextField
            margin="dense"
            label="Machine ID"
            fullWidth
            variant="outlined"
            value={newAgentMachineId}
            onChange={(e) => setNewAgentMachineId(e.target.value)}
            sx={{ mb: 2 }}
            required
            helperText="Unique identifier for the machine (required)"
          />
          <TextField
            margin="dense"
            label="Machine Name (Optional)"
            fullWidth
            variant="outlined"
            value={newAgentMachineName}
            onChange={(e) => setNewAgentMachineName(e.target.value)}
            sx={{ mb: 2 }}
            helperText="Human-readable name for the machine"
          />
          <TextField
            margin="dense"
            label="IP Address (Optional)"
            fullWidth
            variant="outlined"
            value={newAgentIpAddress}
            onChange={(e) => setNewAgentIpAddress(e.target.value)}
            sx={{ mb: 2 }}
            helperText="IP address of the machine"
          />
          <TextField
            margin="dense"
            label="Description (Optional)"
            fullWidth
            variant="outlined"
            multiline
            rows={3}
            value={newAgentDescription}
            onChange={(e) => setNewAgentDescription(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setCreateDialogOpen(false)
            setNewAgentName('')
            setNewAgentDescription('')
            setNewAgentMachineId('')
            setNewAgentMachineName('')
            setNewAgentIpAddress('')
            setError('')
          }}>Cancel</Button>
          <Button onClick={handleCreateAgent} variant="contained">
            Create
          </Button>
        </DialogActions>
      </Dialog>

      {/* Provisioning Token Dialog */}
      <Dialog open={provisioningDialogOpen} onClose={() => setProvisioningDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Agent Provisioning Token</DialogTitle>
        <DialogContent>
          <Alert severity="info" sx={{ mb: 2 }}>
            Use this token to register a new Windows agent. The token expires in 24 hours.
          </Alert>
          <TextField
            fullWidth
            multiline
            rows={3}
            value={provisioningToken}
            InputProps={{
              readOnly: true,
              endAdornment: (
                <IconButton onClick={() => copyToClipboard(provisioningToken)}>
                  <ContentCopy />
                </IconButton>
              )
            }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => copyToClipboard(provisioningToken)} startIcon={<ContentCopy />}>
            Copy Token
          </Button>
          <Button onClick={() => setProvisioningDialogOpen(false)} variant="contained">
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

export default AgentsPage
