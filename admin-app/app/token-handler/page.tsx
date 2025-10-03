'use client';

import { useAuth } from '@/components/auth-provider';
import { LoginForm } from '@/components/login-form';
import { Navigation } from '@/components/navigation';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Loader2, ExternalLink, CheckCircle, XCircle } from 'lucide-react';

function TokenHandlerContent() {
  const [redirectUri, setRedirectUri] = useState('http://localhost:3000/oauth/callback');
  const [authUrl, setAuthUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const [loginStatus, setLoginStatus] = useState<{ isLoggedIn: boolean; userId?: string } | null>(null);

  const handleStartLogin = async () => {
    setLoading(true);
    try {
      const response = await fetch('http://localhost:5261/oauth/login/start', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ redirectUri }),
      });
      const data = await response.json();
      setAuthUrl(data.authorizationUrl);
    } catch (error) {
      alert('Error: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const handleCheckStatus = async () => {
    setLoading(true);
    try {
      const response = await fetch('http://localhost:5261/oauth/isloggedin', {
        method: 'GET',
        credentials: 'include',
      });
      const data = await response.json();
      setLoginStatus(data);
    } catch (error) {
      alert('Error: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = async () => {
    setLoading(true);
    try {
      const response = await fetch('http://localhost:5261/oauth/logout', {
        method: 'POST',
        credentials: 'include',
      });
      const data = await response.json();
      alert(data.message);
      setLoginStatus(null);
      setAuthUrl('');
    } catch (error) {
      alert('Error: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-3xl font-bold tracking-tight">Token Handler Pattern Testing</h2>
        <p className="text-muted-foreground">Test OAuth authentication flows and session management</p>
      </div>

      <Tabs defaultValue="login" className="space-y-4">
        <TabsList>
          <TabsTrigger value="login">OAuth Login</TabsTrigger>
          <TabsTrigger value="status">Session Status</TabsTrigger>
          <TabsTrigger value="docs">Documentation</TabsTrigger>
        </TabsList>

        <TabsContent value="login" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Start OAuth Login Flow</CardTitle>
              <CardDescription>Initiate the OAuth authorization flow</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-2">
                <Label htmlFor="redirectUri">Redirect URI</Label>
                <Input
                  id="redirectUri"
                  value={redirectUri}
                  onChange={(e) => setRedirectUri(e.target.value)}
                  placeholder="http://localhost:3000/oauth/callback"
                />
              </div>
              <Button onClick={handleStartLogin} disabled={loading}>
                {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Start Login
              </Button>
              {authUrl && (
                <div className="space-y-2">
                  <Label>Authorization URL</Label>
                  <div className="flex gap-2">
                    <Input value={authUrl} readOnly className="font-mono text-sm" />
                    <Button
                      variant="outline"
                      onClick={() => window.open(authUrl, '_blank')}
                    >
                      <ExternalLink className="h-4 w-4" />
                    </Button>
                  </div>
                  <p className="text-sm text-muted-foreground">
                    Click the button to open the authorization URL in a new tab
                  </p>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="status" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Check Login Status</CardTitle>
              <CardDescription>Verify the current authentication status</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex gap-2">
                <Button onClick={handleCheckStatus} disabled={loading}>
                  {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Check Status
                </Button>
                {loginStatus?.isLoggedIn && (
                  <Button variant="destructive" onClick={handleLogout} disabled={loading}>
                    {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                    Logout
                  </Button>
                )}
              </div>
              {loginStatus && (
                <Card>
                  <CardContent className="pt-6">
                    <div className="flex items-center gap-2">
                      {loginStatus.isLoggedIn ? (
                        <CheckCircle className="h-5 w-5 text-green-600" />
                      ) : (
                        <XCircle className="h-5 w-5 text-red-600" />
                      )}
                      <span className="font-medium">
                        {loginStatus.isLoggedIn ? 'Logged In' : 'Not Logged In'}
                      </span>
                    </div>
                    {loginStatus.userId && (
                      <p className="mt-2 text-sm text-muted-foreground">
                        User ID: {loginStatus.userId}
                      </p>
                    )}
                  </CardContent>
                </Card>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="docs" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>OAuth Flow Overview</CardTitle>
              <CardDescription>Understanding the token handler pattern</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <h3 className="font-semibold mb-2">1. Start Login</h3>
                <p className="text-sm text-muted-foreground">
                  POST /oauth/login/start with a redirect URI. Returns an authorization URL.
                </p>
              </div>
              <div>
                <h3 className="font-semibold mb-2">2. User Authentication</h3>
                <p className="text-sm text-muted-foreground">
                  User is redirected to the authorization server to authenticate.
                </p>
              </div>
              <div>
                <h3 className="font-semibold mb-2">3. Callback</h3>
                <p className="text-sm text-muted-foreground">
                  Authorization server redirects back to your callback URL with an authorization code.
                </p>
              </div>
              <div>
                <h3 className="font-semibold mb-2">4. Token Exchange</h3>
                <p className="text-sm text-muted-foreground">
                  The gateway exchanges the code for tokens and creates a session.
                </p>
              </div>
              <div>
                <h3 className="font-semibold mb-2">5. Check Status</h3>
                <p className="text-sm text-muted-foreground">
                  GET /oauth/isloggedin to verify authentication status.
                </p>
              </div>
              <div>
                <h3 className="font-semibold mb-2">6. Logout</h3>
                <p className="text-sm text-muted-foreground">
                  POST /oauth/logout to revoke the session and clear cookies.
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

export default function TokenHandlerPage() {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <LoginForm />;
  return (
    <div className="flex h-screen">
      <aside className="w-64 border-r bg-card"><Navigation /></aside>
      <main className="flex-1 overflow-auto">
        <div className="container mx-auto p-6"><TokenHandlerContent /></div>
      </main>
    </div>
  );
}
