# Profiles — M0 identity with P4 campaigns awaiting local validation

`ProfileStore` retains GUID + display name in the existing `user://profiles.cfg` ConfigFile format. Creation/selection alone does not generate a world. Registry writes now keep a `.bak`; loading tries it if the primary cannot be read, and failed reads block creation rather than overwriting unreadable data. Recovered invalid primaries are quarantined on the next successful write.

CampaignStore separately associates each profile with one active RunId and independent WorldId under `user://saves`. New Campaign confirms changing that active selection; previous run files are retained. There is no slot/archive browser or permadeath cleanup. Renaming/display names never identify world files. See [../README.md](../README.md).
