# Removable Chat ASCII console

LocalGPT 2.2.10 fixes the in-chat ASCII game surface becoming effectively permanent after it occupied the conversation area.

The console header now contains its own **Close** button. It is present before a game is started and while a game is active, and remains reachable in responsive layouts and all fullscreen scale modes: Fit, Width and Native.

When the console is fullscreen, LocalGPT first exits the browser Fullscreen API and only then asks the Chat page to remove the component. The Chat grid immediately releases the reserved game row and the normal conversation viewport becomes visible again. A browser refresh or session rejoin is no longer required.

Closing the surface does not delete the authoritative game session. Selecting **Show ASCII games** later mounts the console again and reconnects it to the active conversation game where available.
