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
  Paper,
  FormControl,
  InputLabel,
  Select,
  OutlinedInput,
  Grid
} from '@mui/material'
import {
  Add,
  MoreVert,
  People,
  Edit,
  Delete,
  Refresh,
  Block,
  CheckCircle
} from '@mui/icons-material'
import { usersApi } from '../../services/usersApi'
import { User, CreateUserRequest, UpdateUserRequest, UserRole } from '../../types'
import { useAuthStore } from '../../stores/authStore'

const ROLES = ['SystemAdmin', 'TenantAdmin', 'Operator']

const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<User | null>(null)
  const [formData, setFormData] = useState({
    email: '',
    firstName: '',
    lastName: '',
    role: '',
    password: '',
    confirmPassword: ''
  })
  const [error, setError] = useState('')
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [selectedUser, setSelectedUser] = useState<User | null>(null)
  const { user: currentUser } = useAuthStore()

  const fetchUsers = async () => {
    try {
      setLoading(true)
      const response = await usersApi.getUsers(1, 100)
      setUsers(response.items)
    } catch (error) {
      console.error('Failed to fetch users:', error)
      setError('Failed to fetch users')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchUsers()
  }, [])

  const resetForm = () => {
    setFormData({
      email: '',
      firstName: '',
      lastName: '',
      role: '',
      password: '',
      confirmPassword: ''
    })
    setEditingUser(null)
  }

  const handleCreateUser = async () => {
    if (!formData.email.trim() || !formData.firstName.trim() || !formData.lastName.trim()) {
      setError('Email, first name, and last name are required')
      return
    }

    if (!formData.password || formData.password !== formData.confirmPassword) {
      setError('Password confirmation does not match')
      return
    }

    if (!formData.role) {
      setError('Role is required')
      return
    }

    try {
      const newUser: CreateUserRequest = {
        email: formData.email.trim(),
        firstName: formData.firstName.trim(),
        lastName: formData.lastName.trim(),
        password: formData.password,
        role: formData.role as UserRole
      }

      await usersApi.createUser(newUser)
      setCreateDialogOpen(false)
      resetForm()
      setError('')
      fetchUsers()
    } catch (error: any) {
      setError(error.response?.data?.message || 'Failed to create user')
    }
  }

  const handleEditUser = (user: User) => {
    setEditingUser(user)
    setFormData({
      email: user.email,
      firstName: user.firstName,
      lastName: user.lastName,
      role: user.role,
      password: '',
      confirmPassword: ''
    })
    setEditDialogOpen(true)
    handleMenuClose()
  }

  const handleUpdateUser = async () => {
    if (!editingUser) return

    if (!formData.email.trim() || !formData.firstName.trim() || !formData.lastName.trim()) {
      setError('Email, first name, and last name are required')
      return
    }

    if (formData.password && formData.password !== formData.confirmPassword) {
      setError('Password confirmation does not match')
      return
    }

    if (!formData.role) {
      setError('Role is required')
      return
    }

    try {
      const updateData: UpdateUserRequest = {
        email: formData.email.trim(),
        firstName: formData.firstName.trim(),
        lastName: formData.lastName.trim(),
        role: formData.role
      }

      await usersApi.updateUser(editingUser.id, updateData)
      setEditDialogOpen(false)
      resetForm()
      setError('')
      fetchUsers()
    } catch (error: any) {
      setError(error.response?.data?.message || 'Failed to update user')
    }
  }

  const handleDeleteUser = async (user: User) => {
    if (user.id === currentUser?.id) {
      setError('You cannot delete your own account')
      return
    }

    if (window.confirm(`Are you sure you want to delete user "${user.email}"?`)) {
      try {
        await usersApi.deleteUser(user.id)
        fetchUsers()
        handleMenuClose()
      } catch (error: any) {
        setError(error.response?.data?.message || 'Failed to delete user')
      }
    }
  }

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, user: User) => {
    setAnchorEl(event.currentTarget)
    setSelectedUser(user)
  }

  const handleMenuClose = () => {
    setAnchorEl(null)
    setSelectedUser(null)
  }

  const getRoleColor = (role: string) => {
    switch (role) {
      case 'SystemAdmin':
        return 'error'
      case 'TenantAdmin':
        return 'warning'
      case 'Operator':
        return 'primary'
      default:
        return 'default'
    }
  }

  const canEditUser = (user: User) => {
    return currentUser?.role === 'SystemAdmin' || currentUser?.id === user.id
  }

  const canDeleteUser = (user: User) => {
    return currentUser?.role === 'SystemAdmin' && currentUser?.id !== user.id
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight="bold">
          Users
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {currentUser?.role === 'SystemAdmin' && (
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setCreateDialogOpen(true)}
            >
              Add User
            </Button>
          )}
          <Tooltip title="Refresh">
            <IconButton onClick={fetchUsers} disabled={loading}>
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
      ) : users.length === 0 ? (
        <Box sx={{ textAlign: 'center', py: 4 }}>
          <People sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
          <Typography variant="h6" color="text.secondary" gutterBottom>
            No users found
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Add your first user to get started
          </Typography>
          {currentUser?.role === 'SystemAdmin' && (
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setCreateDialogOpen(true)}
            >
              Add User
            </Button>
          )}
        </Box>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Roles</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Created</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {users.map((user) => (
                <TableRow key={user.id}>
                  <TableCell>
                    {user.firstName} {user.lastName}
                  </TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>
                    <Chip
                      label={user.role}
                      color={getRoleColor(user.role) as any}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={user.isActive ? 'Active' : 'Inactive'}
                      color={user.isActive ? 'success' : 'default'}
                      size="small"
                      icon={user.isActive ? <CheckCircle /> : <Block />}
                    />
                  </TableCell>
                  <TableCell>
                    {new Date(user.createdAt).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    {(canEditUser(user) || canDeleteUser(user)) && (
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuClick(e, user)}
                      >
                        <MoreVert />
                      </IconButton>
                    )}
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
        {selectedUser && canEditUser(selectedUser) && (
          <MenuItem onClick={() => handleEditUser(selectedUser)}>
            <Edit sx={{ mr: 1 }} />
            Edit
          </MenuItem>
        )}
        {selectedUser && canDeleteUser(selectedUser) && (
          <MenuItem onClick={() => selectedUser && handleDeleteUser(selectedUser)}>
            <Delete sx={{ mr: 1 }} />
            Delete
          </MenuItem>
        )}
      </Menu>

      {/* Create User Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add New User</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Email"
            type="email"
            fullWidth
            variant="outlined"
            value={formData.email}
            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            sx={{ mb: 2 }}
          />
          <Grid container spacing={2} sx={{ mb: 2 }}>
            <Grid item xs={6}>
              <TextField
                margin="dense"
                label="First Name"
                fullWidth
                variant="outlined"
                value={formData.firstName}
                onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField
                margin="dense"
                label="Last Name"
                fullWidth
                variant="outlined"
                value={formData.lastName}
                onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
              />
            </Grid>
          </Grid>
          <FormControl fullWidth margin="dense" sx={{ mb: 2 }}>
            <InputLabel>Role</InputLabel>
            <Select
              value={formData.role}
              onChange={(e) => setFormData({ ...formData, role: e.target.value as string })}
              input={<OutlinedInput label="Role" />}
            >
              {ROLES.map((role) => (
                <MenuItem key={role} value={role}>
                  {role}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            margin="dense"
            label="Password"
            type="password"
            fullWidth
            variant="outlined"
            value={formData.password}
            onChange={(e) => setFormData({ ...formData, password: e.target.value })}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Confirm Password"
            type="password"
            fullWidth
            variant="outlined"
            value={formData.confirmPassword}
            onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleCreateUser} variant="contained">
            Create User
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit User Dialog */}
      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit User</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Email"
            type="email"
            fullWidth
            variant="outlined"
            value={formData.email}
            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            sx={{ mb: 2 }}
          />
          <Grid container spacing={2} sx={{ mb: 2 }}>
            <Grid item xs={6}>
              <TextField
                margin="dense"
                label="First Name"
                fullWidth
                variant="outlined"
                value={formData.firstName}
                onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
              />
            </Grid>
            <Grid item xs={6}>
              <TextField
                margin="dense"
                label="Last Name"
                fullWidth
                variant="outlined"
                value={formData.lastName}
                onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
              />
            </Grid>
          </Grid>
          <FormControl fullWidth margin="dense" sx={{ mb: 2 }}>
            <InputLabel>Role</InputLabel>
            <Select
              value={formData.role}
              onChange={(e) => setFormData({ ...formData, role: e.target.value as string })}
              input={<OutlinedInput label="Role" />}
            >
              {ROLES.map((role) => (
                <MenuItem key={role} value={role}>
                  {role}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            margin="dense"
            label="New Password (Optional)"
            type="password"
            fullWidth
            variant="outlined"
            value={formData.password}
            onChange={(e) => setFormData({ ...formData, password: e.target.value })}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Confirm New Password"
            type="password"
            fullWidth
            variant="outlined"
            value={formData.confirmPassword}
            onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleUpdateUser} variant="contained">
            Update User
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

export default UsersPage
