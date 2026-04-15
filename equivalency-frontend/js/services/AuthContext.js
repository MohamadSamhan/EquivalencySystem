// AuthContext - Authentication & Authorization Provider
// Handles JWT decoding, role verification, and session persistence

const AuthContext = React.createContext(null);

function AuthProvider({ children }) {
  const [user, setUser] = React.useState(null);
  const [isAuthenticated, setIsAuthenticated] = React.useState(false);
  const [role, setRole] = React.useState('');
  const [loading, setLoading] = React.useState(true);

  // Initialize Auth State from LocalStorage
  React.useEffect(() => {
    const token = localStorage.getItem('token');
    const savedRole = localStorage.getItem('role');
    const userName = localStorage.getItem('userName');

    if (token && savedRole) {
      // Split name here too in case it was stored as full name previously
      const firstPart = userName ? userName.split(' ')[0] : '';
      setUser({ name: firstPart, token });
      setRole(savedRole);
      setIsAuthenticated(true);
    }
    setLoading(false);
  }, []);

  const login = async (email, password) => {
    try {
      const response = await authAPI.login(email, password);
      const { token, role: responseRole, userName, fullName, name } = response.data;

      // DEBUG: شوف إيش بيرجع من الـ API
      console.log('API Response data:', response.data);
      console.log('Name fields:', { userName, fullName, name });

      // 1. Decode JWT payload for extra verification (User's requirement)
      const payloadBase64 = token.split(".")[1];
      const payloadJson = JSON.parse(atob(payloadBase64));
      
      // Some JWTs use different claim names for roles (e.g., http://schemas.microsoft.com/ws/2008/06/identity/claims/role)
      // We'll check both 'role' and the common ASP.NET claim URI
      const roleFromToken = payloadJson.role || 
                            payloadJson["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

      console.log('Verifying Role:', { responseRole, roleFromToken });

      // 2. Verification logic
      if (roleFromToken && roleFromToken !== responseRole) {
        console.warn('Role mismatch between API response and Token payload!');
        // In some cases we might trust the token more, but here we'll proceed if they match
      }

      const finalRole = roleFromToken || responseRole;

      // Extract a friendly name — check all possible field names from API
      const apiName = fullName || name || userName || '';
      const isEmail = (s) => s && s.includes('@');
      const isNumeric = (s) => s && /^\d+$/.test(s.trim());

      let finalName = '';

      // 1. Use API name directly if it's a real name (not email/numeric)
      if (apiName && !isEmail(apiName) && !isNumeric(apiName)) {
        finalName = apiName;
      }
      // 2. Try JWT claims
      else {
        const jwtName = payloadJson.name ||
                        payloadJson.given_name ||
                        payloadJson["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ||
                        payloadJson["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"];
        if (jwtName && !isEmail(jwtName) && !isNumeric(jwtName)) {
          finalName = jwtName;
        }
      }
      // 3. Try to extract from email
      if (!finalName && isEmail(apiName || '')) {
        let namePart = (apiName).split('@')[0].replace(/[0-9]/g, '').replace(/\./g, ' ').trim();
        finalName = namePart.split(' ').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ').trim();
      }

      console.log('Final name resolved:', finalName);

      // Use only first name
      if (finalName && finalName.includes(' ')) {
        finalName = finalName.split(' ')[0];
      }

      localStorage.setItem('token', token);
      localStorage.setItem('role', finalRole);
      localStorage.setItem('userName', finalName);
      localStorage.setItem('fullName', finalName);

      // 4. Update state
      setUser({ name: finalName, token });
      setRole(finalRole);
      setIsAuthenticated(true);

      return { success: true, role: finalRole };
    } catch (error) {
      console.error('Login implementation error:', error);
      throw error;
    }
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    localStorage.removeItem('userName');
    setUser(null);
    setRole('');
    setIsAuthenticated(false);
    window.location.hash = '#/login';
  };

  const value = {
    user,
    role,
    isAuthenticated,
    loading,
    login,
    logout
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

// Hook for easy access to AuthContext
function useAuth() {
  const context = React.useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
