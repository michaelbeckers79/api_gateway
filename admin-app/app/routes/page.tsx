'use client';

import { useAuth } from '@/components/auth-provider';
import { LoginForm } from '@/components/login-form';
import { Navigation } from '@/components/navigation';
import { useEffect, useState } from 'react';
import { apiClient, type RouteConfig } from '@/lib/api-client';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Plus, Pencil, Trash2, Loader2 } from 'lucide-react';

function RouteDialog({ route, onSave, onClose }: { route?: RouteConfig; onSave: () => void; onClose: () => void }) {
  const [loading, setLoading] = useState(false);
  const [routeId, setRouteId] = useState(route?.routeId || '');
  const [clusterId, setClusterId] = useState(route?.clusterId || '');
  const [match, setMatch] = useState(route?.match || '');
  const [order, setOrder] = useState(route?.order.toString() || '1');
  const [isActive, setIsActive] = useState(route?.isActive ?? true);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const data = { routeId, clusterId, match, order: parseInt(order), isActive };
      if (route) {
        await apiClient.updateRoute(route.id, data);
      } else {
        await apiClient.createRoute(data);
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
        <DialogTitle>{route ? 'Edit Route' : 'Create Route'}</DialogTitle>
        <DialogDescription>
          {route ? 'Update route configuration' : 'Add a new route to the gateway'}
        </DialogDescription>
      </DialogHeader>
      <form onSubmit={handleSubmit}>
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="routeId">Route ID</Label>
            <Input id="routeId" value={routeId} onChange={(e) => setRouteId(e.target.value)} required />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="clusterId">Cluster ID</Label>
            <Input id="clusterId" value={clusterId} onChange={(e) => setClusterId(e.target.value)} required />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="match">Match Pattern</Label>
            <Input id="match" value={match} onChange={(e) => setMatch(e.target.value)} placeholder="/api/{**catch-all}" required />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="order">Order</Label>
            <Input id="order" type="number" value={order} onChange={(e) => setOrder(e.target.value)} required />
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
            {route ? 'Update' : 'Create'}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  );
}

function RoutesContent() {
  const [routes, setRoutes] = useState<RouteConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingRoute, setEditingRoute] = useState<RouteConfig | undefined>();

  const loadRoutes = async () => {
    try {
      const data = await apiClient.getRoutes();
      setRoutes(data);
    } catch (error) {
      alert('Error loading routes: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadRoutes();
  }, []);

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure you want to delete this route?')) return;
    try {
      await apiClient.deleteRoute(id);
      loadRoutes();
    } catch (error) {
      alert('Error: ' + (error as Error).message);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Routes</h2>
          <p className="text-muted-foreground">Manage API routes and their configurations</p>
        </div>
        <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
          <DialogTrigger asChild>
            <Button onClick={() => { setEditingRoute(undefined); setDialogOpen(true); }}>
              <Plus className="mr-2 h-4 w-4" />Add Route
            </Button>
          </DialogTrigger>
          {dialogOpen && <RouteDialog route={editingRoute} onSave={loadRoutes} onClose={() => setDialogOpen(false)} />}
        </Dialog>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>All Routes</CardTitle>
          <CardDescription>List of configured routes in the gateway</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          ) : routes.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">No routes configured</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Route ID</TableHead>
                  <TableHead>Cluster ID</TableHead>
                  <TableHead>Match Pattern</TableHead>
                  <TableHead>Order</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {routes.map((route) => (
                  <TableRow key={route.id}>
                    <TableCell className="font-medium">{route.routeId}</TableCell>
                    <TableCell>{route.clusterId}</TableCell>
                    <TableCell className="font-mono text-sm">{route.match}</TableCell>
                    <TableCell>{route.order}</TableCell>
                    <TableCell>
                      <span className={`inline-flex items-center rounded-full px-2 py-1 text-xs font-medium ${route.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700'}`}>
                        {route.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button variant="outline" size="sm" onClick={() => { setEditingRoute(route); setDialogOpen(true); }}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button variant="outline" size="sm" onClick={() => handleDelete(route.id)}>
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

export default function RoutesPage() {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <LoginForm />;
  return (
    <div className="flex h-screen">
      <aside className="w-64 border-r bg-card"><Navigation /></aside>
      <main className="flex-1 overflow-auto">
        <div className="container mx-auto p-6"><RoutesContent /></div>
      </main>
    </div>
  );
}
