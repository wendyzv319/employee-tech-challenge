type Listener = () => void;

const listeners: Listener[] = [];

export function onLogout(listener: Listener) {
  listeners.push(listener);
  return () => {
    const i = listeners.indexOf(listener);
    if (i >= 0) listeners.splice(i, 1);
  };
}

export function emitLogout() {
  listeners.forEach((l) => l());
}
