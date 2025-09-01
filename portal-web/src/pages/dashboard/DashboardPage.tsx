import React, { useState, useEffect } from 'react'
import {
  Box,
  Grid,
  Typography,
  Card,
  CardContent,
  CardHeader,
  List,
  ListItem,
  ListItemText,
  Chip,
  IconButton,
  Tooltip
} from '@mui/material'
import {
  Computer,
  PlayArrow,
  People,
  Refresh,
  TrendingUp
} from '@mui/icons-material'
import { agentsApi } from '../../services/agentsApi'
import { sessionsApi } from '../../services/sessionsApi'
import { usersApi } from '../../services/usersApi'
import { Agent, Session } from '../../types'

interface DashboardStats {
  totalAgents: number
  onlineAgents: number
  activeSessions: number
  totalUsers: number
}

const DashboardPage: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats>({
    totalAgents: 0,
    onlineAgents: 0,
    activeSessions: 0,
    totalUsers: 0
  })
  const [recentSessions, setRecentSessions] = useState<Session[]>([])
  const [recentAgents, setRecentAgents] = useState<Agent[]>([])
  const [loading, setLoading] = useState(true)

  const fetchDashboardData = async () => {
    try {
      setLoading(true)
      
      const [agentsResponse, sessionsResponse, usersResponse] = await Promise.all([
        agentsApi.getAgents(1, 10),
        sessionsApi.getSessions(1, 10),
        usersApi.getUsers(1, 10)
      ])

      const onlineAgents = agentsResponse.items.filter(
        agent => agent.status === 'online'
      ).length

      const activeSessions = sessionsResponse.items.filter(
        session => session.status === 'active'
      ).length

      setStats({
        totalAgents: agentsResponse.total,
        onlineAgents,
        activeSessions,
        totalUsers: usersResponse.total
      })

      setRecentSessions(sessionsResponse.items.slice(0, 5))
      setRecentAgents(agentsResponse.items.slice(0, 5))
    } catch (error) {
      console.error('Failed to fetch dashboard data:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchDashboardData()
  }, [])

  const StatCard: React.FC<{
    title: string
    value: number
    icon: React.ReactNode
    color: 'primary' | 'secondary' | 'success' | 'warning'
  }> = ({ title, value, icon, color }) => (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box>
            <Typography color="text.secondary" variant="h6" gutterBottom>
              {title}
            </Typography>
            <Typography variant="h3" color={`${color}.main`} fontWeight="bold">
              {value}
            </Typography>
          </Box>
          <Box sx={{ color: `${color}.main`, fontSize: 48 }}>
            {icon}
          </Box>
        </Box>
      </CardContent>
    </Card>
  )

  const getSessionStatusColor = (status: string) => {
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

  const getAgentStatusColor = (status: string) => {
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

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight="bold">
          Dashboard
        </Typography>
        <Tooltip title="Refresh">
          <IconButton onClick={fetchDashboardData} disabled={loading}>
            <Refresh />
          </IconButton>
        </Tooltip>
      </Box>

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Total Agents"
            value={stats.totalAgents}
            icon={<Computer />}
            color="primary"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Online Agents"
            value={stats.onlineAgents}
            icon={<TrendingUp />}
            color="success"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Active Sessions"
            value={stats.activeSessions}
            icon={<PlayArrow />}
            color="warning"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Total Users"
            value={stats.totalUsers}
            icon={<People />}
            color="secondary"
          />
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} lg={6}>
          <Card>
            <CardHeader title="Recent Sessions" />
            <CardContent>
              {recentSessions.length === 0 ? (
                <Typography color="text.secondary" textAlign="center" py={2}>
                  No recent sessions
                </Typography>
              ) : (
                <List>
                  {recentSessions.map((session, index) => (
                    <ListItem key={session.id} divider={index < recentSessions.length - 1}>
                      <ListItemText
                        primary={`${session.agentName} - ${session.username}`}
                        secondary={`Connected: ${new Date(session.createdAt).toLocaleString()}`}
                      />
                      <Chip
                        label={session.status}
                        color={getSessionStatusColor(session.status) as any}
                        size="small"
                      />
                    </ListItem>
                  ))}
                </List>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} lg={6}>
          <Card>
            <CardHeader title="Recent Agents" />
            <CardContent>
              {recentAgents.length === 0 ? (
                <Typography color="text.secondary" textAlign="center" py={2}>
                  No agents registered
                </Typography>
              ) : (
                <List>
                  {recentAgents.map((agent, index) => (
                    <ListItem key={agent.id} divider={index < recentAgents.length - 1}>
                      <ListItemText
                        primary={agent.name}
                        secondary={`${agent.hostname} - Last seen: ${
                          agent.lastSeen ? new Date(agent.lastSeen).toLocaleString() : 'Never'
                        }`}
                      />
                      <Chip
                        label={agent.status}
                        color={getAgentStatusColor(agent.status) as any}
                        size="small"
                      />
                    </ListItem>
                  ))}
                </List>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  )
}

export default DashboardPage
