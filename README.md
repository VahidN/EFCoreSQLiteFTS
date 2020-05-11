# Implementing full-text search with SQLite and EF-Core

This sample includes:

- How to define FTS5 virtual tables.
- How to update FTS5 virtual tables automatically using the EF-Core's change tracking system after each insert/update/delete.
- How to query FTS5 virtual tables with EF-Core's key-less entities.
- How to use the SQLite's `spellfix1` extension to improve the FTS experience.
