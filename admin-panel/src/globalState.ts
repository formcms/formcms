import useSWR from 'swr';

enum GlobalStateKeys {
    ActiveMenu = 'activeMenu',
    Layout = 'layout',
    Language = 'language',
}

// In-memory store for global state
const globalState: Partial<Record<GlobalStateKeys, any>> = {};

// Fetcher function retrieves from localStorage or memory
const fetcher = (key: GlobalStateKeys) => {
    if (globalState[key] !== undefined) {
        return globalState[key];
    }
    if (typeof window !== 'undefined') {
        const storedValue = localStorage.getItem(key);
        return storedValue ? JSON.parse(storedValue) : null;
    }
    return null;
};

// Define the hook with TypeScript generics
function useGlobalState<T>(key: GlobalStateKeys, initialData: T): [T, (newValue: T) => void] {
    const { data, mutate } = useSWR(key, fetcher, {
        fallbackData: initialData, // Use initial data if cache is empty
    });

    // Update the global state, cache, and localStorage
    const setGlobalState = (newValue: T) => {
        globalState[key] = newValue;
        if (typeof window !== 'undefined') {
            localStorage.setItem(key, JSON.stringify(newValue));
        }
        mutate(newValue, false); // Update cache without re-fetching
    };

    return [data as T, setGlobalState];
}

export { useGlobalState, GlobalStateKeys };
