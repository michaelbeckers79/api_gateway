'use client';

import { useAuth } from '@/components/auth-provider';
import { LoginForm } from '@/components/login-form';
import { Navigation } from '@/components/navigation';
import { useEffect, useState } from 'react';
import { apiClient, type User, type SessionToken } from '@/lib/api-client';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Loader2, UserCheck, UserX, Eye, XCircle } from 'lucide-react';

function UserDetailsDialog({ userId, onClose }: { userId: number; onClose: () => void }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadUser = async () => {
      try {
        const data = await apiClient.getUser(userId);
        setUser(data);
      } catch (error) {
        alert('Error loading user: ' + (error as Error).message);
      } finally {
        setLoading(false);
      }
    };
    loadUser();
  }, [userId]);

  const handleRevokeSession = async (sessionId: number) => {
    if (!confirm('Are you sure you want to revoke this session?')) return;
    try {
      await apiClient.revokeSession(sessionId);
      const data = await apiClient.getUser(userId);
      setUser(data);
    } catch (error) {
      alert('Error: ' + (error as Error).message);
    }
  };

  const handleRevokeAll = async () => {
    if (!confirm('Are you sure you want to revoke all sessions for this user?')) return;
    try {
      await apiClient.revokeAllUserSessions(userId);
      const data = await apiClient.getUser(userId);
      setUser(data);
    } catch (error) {
      alert('Error: ' + (error as Error).message);
    }
  };

  return (
    <DialogContent className="max-w-4xl">
      <DialogHeader>
        <DialogTitle>User Details</DialogTitle>
        <DialogDescription>View user information and active sessions</DialogDescription>
      </DialogHeader>
      {loading ? (
        <div className="flex items-center justify-center py-8">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : user ? (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p className="text-sm font-medium text-muted-foreground">Username</p>
              <p className="text-sm">{user.username}</p>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Email</p>
              <p className="text-sm">{user.email}</p>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Status</p>
              <p className="text-sm">{user.isEnabled ? 'Enabled' : 'Disabled'}</p>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Last Login</p>
              <p className="text-sm">{user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'Never'}</p>
            </div>
          </div>
          <div>
            <div className="flex items-center justify-between mb-2">
              <h3 className="text-lg font-semibold">Active Sessions</h3>
              {user.sessions && user.sessions.length > 0 && (
                <Button size="sm" variant="destructive" onClick={handleRevokeAll}>Revoke All</Button>
              )}
            </div>
            {user.sessions && user.sessions.length > 0 ? (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>IP Address</TableHead>
                    <TableHead>User Agent</TableHead>
                    <TableHead>Last Accessed</TableHead>
                    <TableHead>Expires</TableHead>
                    <TableHead>Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {user.sessions.map((session: SessionToken) => (
                    <TableRow key={session.id}>
                      <TableCell className="font-mono text-sm">{session.ipAddress}</TableCell>
                      <TableCell className="text-sm">{session.userAgent.substring(0, 50)}...</TableCell>
                      <TableCell className="text-sm">{new Date(session.lastAccessedAt).toLocaleString()}</TableCell>
                      <TableCell className="text-sm">{new Date(session.expiresAt).toLocaleString()}</TableCell>
                      <TableCell>
                        <Button size="sm" variant="outline" onClick={() => handleRevokeSession(session.id)}>
                          <XCircle className="h-4 w-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            ) : (
              <p className="text-sm text-muted-foreground">No active sessions</p>
            )}
          </div>
        </div>
      ) : (
        <p className="text-center py-8 text-muted-foreground">User not found</p>
      )}
      <DialogFooter>
        <Button variant="outline" onClick={onClose}>Close</Button>
      </DialogFooter>
    </DialogContent>
  );
}

function UsersContent() {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedUserId, setSelectedUserId] = useState<number | null>(null);

  const loadUsers = async () => {
    try {
      const data = await apiClient.getUsers();
      setUsers(data);
    } catch (error) {
      alert('Error loading users: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadUsers();
  }, []);

  const handleToggleUser = async (id: number, isEnabled: boolean) => {
    try {
      if (isEnabled) {
        await apiClient.disableUser(id);
      } else {
        await apiClient.enableUser(id);
      }
      loadUsers();
    } catch (error) {
      alert('Error: ' + (error as Error).message);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-3xl font-bold tracking-tight">Users</h2>
        <p className="text-muted-foreground">View and manage user accounts and sessions</p>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>All Users</CardTitle>
          <CardDescription>List of registered users</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          ) : users.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">No users found</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Username</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Active Sessions</TableHead>
                  <TableHead>Last Login</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell className="font-medium">{user.username}</TableCell>
                    <TableCell>{user.email}</TableCell>
                    <TableCell>
                      <span className={`inline-flex items-center rounded-full px-2 py-1 text-xs font-medium ${user.isEnabled ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700'}`}>
                        {user.isEnabled ? 'Enabled' : 'Disabled'}
                      </span>
                    </TableCell>
                    <TableCell>{user.activeSessions || 0}</TableCell>
                    <TableCell className="text-sm">{user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleDateString() : 'Never'}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button variant="outline" size="sm" onClick={() => setSelectedUserId(user.id)}>
                          <Eye className="h-4 w-4" />
                        </Button>
                        <Button variant="outline" size="sm" onClick={() => handleToggleUser(user.id, user.isEnabled)}>
                          {user.isEnabled ? <UserX className="h-4 w-4" /> : <UserCheck className="h-4 w-4" />}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
      {selectedUserId && (
        <Dialog open={!!selectedUserId} onOpenChange={() => setSelectedUserId(null)}>
          <UserDetailsDialog userId={selectedUserId} onClose={() => { setSelectedUserId(null); loadUsers(); }} />
        </Dialog>
      )}
    </div>
  );
}

export default function UsersPage() {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <LoginForm />;
  return (
    <div className="flex h-screen">
      <aside className="w-64 border-r bg-card"><Navigation /></aside>
      <main className="flex-1 overflow-auto">
        <div className="container mx-auto p-6"><UsersContent /></div>
      </main>
    </div>
  );
}
