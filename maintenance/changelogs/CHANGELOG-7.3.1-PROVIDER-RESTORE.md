# LocalGPT 7.3.1

- 7.3.0 stored the chosen chat provider but the provider list is rebuilt after loading (adding the AI Council session), which reset the choice again. The rebuild now prefers the remembered provider until you pick another one.
- The provider choice is also saved directly from the provider dropdown (the earlier hook did not fire there).
