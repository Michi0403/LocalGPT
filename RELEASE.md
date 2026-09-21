# LocalGPT 4.6.9

LocalGPT 4.6.9 replaces the permanent `/chat` Upload advisor panel with an idle-invisible external-file drag/drop landing surface modeled on the existing PublisherStudio interaction pattern.

Nothing is added to normal Chat document flow while the user is not dragging files. During an external file drag, a temporary viewport overlay is positioned over the Local Chat surface. A completed drop is streamed through a dedicated bounded multipart endpoint into the existing LocalGPT quarantine workspace service. The existing Chat paperclip attachment path remains separate and unchanged.

After quarantine succeeds, the existing ingestion inspection and recommendation workflow runs and only then opens the review popup. The drop itself does not promote, build, publish, teach Knowledge or execute uploaded material; those existing gates remain authoritative.

The 4.6.8 Kernel Creature Tournament, ASCII/Pixel, rejoin and terminal-layout repairs are preserved. PublisherStudio is unchanged in this release.
