'use client';

import { useAuth } from '@/components/auth-provider';
import { LoginForm } from '@/components/login-form';
import { Navigation } from '@/components/navigation';
import { useEffect, useState } from 'react';
import { apiClient, type ClusterConfig } from '@/lib/api-client';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Plus, Pencil, Trash2, Loader2 } from 'lucide-react';

function ClusterDialog({ cluster, onSave, onClose }: { cluster?: ClusterConfig; onSave: () => void; onClose: () => void }) {
  const [loading, setLoading] = useState(false);
  const [clusterId, setClusterId] = useState(cluster?.clusterId || '');
  const [destinationAddress, setDestinationAddress] = useState(cluster?.destinationAddress || '');
  const [isActive, setIsActive] = useState(cluster?.isActive ?? true);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const data = { clusterId, destinationAddress, isActive };
      if (cluster) {
        await apiClient.updateCluster(cluster.id, data);
      } else {
        await apiClient.createCluster(data);
      }
      onSave();
      onClose();
    } catch (error) {
      alert('Error: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <DialogContent>
      <DialogHeader>
        <DialogTitle>{cluster ? 'Edit Cluster' : 'Create Cluster'}</DialogTitle>
        <DialogDescription>{cluster ? 'Update cluster configuration' : 'Add a new backend cluster'}</DialogDescription>
      </DialogHeader>
      <form onSubmit={handleSubmit}>
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="clusterId">Cluster ID</Label>
            <Input id="clusterId" value={clusterId} onChange={(e) => setClusterId(e.target.value)} required />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="destinationAddress">Destination Address</Label>
            <Input id="destinationAddress" value={destinationAddress} onChange={(e) => setDestinationAddress(e.target.value)} placeholder="http://localhost:5001" required />
          </div>
          <div className="flex items-center gap-2">
            <input id="isActive" type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} className="h-4 w-4" />
            <Label htmlFor="isActive">Active</Label>
          </div>
        </div>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose}>Cancel</Button>
          <Button type="submit" disabled={loading}>
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {cluster ? 'Update' : 'Create'}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  );
}

function ClustersContent() {
  const [clusters, setClusters] = useState<ClusterConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingCluster, setEditingCluster] = useState<ClusterConfig | undefined>();

  const loadClusters = async () => {
    try {
      const data = await apiClient.getClusters();
      setClusters(data);
    } catch (error) {
      alert('Error loading clusters: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadClusters();
  }, []);

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure you want to delete this cluster?')) return;
    try {
      await apiClient.deleteCluster(id);
      loadClusters();
    } catch (error) {
      alert('Error: ' + (error as Error).message);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Clusters</h2>
          <p className="text-muted-foreground">Configure backend server clusters</p>
        </div>
        <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
          <DialogTrigger asChild>
            <Button onClick={() => { setEditingCluster(undefined); setDialogOpen(true); }}>
              <Plus className="mr-2 h-4 w-4" />Add Cluster
            </Button>
          </DialogTrigger>
          {dialogOpen && <ClusterDialog cluster={editingCluster} onSave={loadClusters} onClose={() => setDialogOpen(false)} />}
        </Dialog>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>All Clusters</CardTitle>
          <CardDescription>List of configured backend clusters</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          ) : clusters.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">No clusters configured</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Cluster ID</TableHead>
                  <TableHead>Destination Address</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {clusters.map((cluster) => (
                  <TableRow key={cluster.id}>
                    <TableCell className="font-medium">{cluster.clusterId}</TableCell>
                    <TableCell className="font-mono text-sm">{cluster.destinationAddress}</TableCell>
                    <TableCell>
                      <span className={`inline-flex items-center rounded-full px-2 py-1 text-xs font-medium ${cluster.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700'}`}>
                        {cluster.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button variant="outline" size="sm" onClick={() => { setEditingCluster(cluster); setDialogOpen(true); }}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button variant="outline" size="sm" onClick={() => handleDelete(cluster.id)}>
                          <Trash2 className="h-4 w-4" />
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
    </div>
  );
}

export default function ClustersPage() {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <LoginForm />;
  return (
    <div className="flex h-screen">
      <aside className="w-64 border-r bg-card"><Navigation /></aside>
      <main className="flex-1 overflow-auto">
        <div className="container mx-auto p-6"><ClustersContent /></div>
      </main>
    </div>
  );
}
