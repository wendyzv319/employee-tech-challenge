import { useEffect, useMemo, useState } from "react";
import Login from "./pages/Login";
import Employees from "./pages/Employees";
import { getAuthToken, setAuthToken } from "./services/api";
import { onLogout } from "./services/authEvents";

function App() {
  const initial = useMemo(() => Boolean(getAuthToken()), []);
  const [isLogged, setIsLogged] = useState(initial);

  function handleLogout() {
    setAuthToken(null);
    setIsLogged(false);
  }

  useEffect(() => {
    return onLogout(() => {
      setIsLogged(false);
    });
  }, []);

  return isLogged ? (
    <Employees onLogout={handleLogout} />
  ) : (
    <Login onLoggedIn={() => setIsLogged(true)} />
  );
}

export default App;

