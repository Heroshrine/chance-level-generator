#0.8

- Added Room.cs, a class that represents a room in the level. The room contains a method that is called to close doors.
- Added RoomRule.cs, the base class of room rules.
- Added PlacementRule.cs, the class that determines how rooms are placed in a level.

#0.7.3

- Fixed bug where removing the position (0, 0) would break island bridging.
- Fixed bug where collection could modify itself during a foreach loop.