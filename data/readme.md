# Sample musics in the musikmaskin format.

* Notes are seperated by whitespace
* `#` at the start of the line is a comment and is ignored
* Notes are played sequentially, chords is not possible
* `<Note><Octave?><length?>` plays a note
* `-<length?>` causes the play to wait

## Notes
* Notes are C D E F G A B
* Some notes can be changed to sharp with a `#`, a flat `b` or not at all `.` by adding that character after the note

## Octave
* Octave is a number followed by the note specifying the octave
* If not specified, octave 4 is assumed

# Length
* Length part starts with the scope it is applied to
    * `:` is only applied to the current note/wait
    * `!` is applied to the current note/wait and all the following notes/waits
* The length is either a full like `5` or a fraction `1/4`

# Examples

| Code    | Meaning |
|---------|--|
| `D`     | Play D |
| `C:1/6` | Play C, lasting 1/6 |
| `A:6`   | Play A, lasting 6 |
| `-`     | Silence, default length |
| `-:1/6` | Silence for 1/6 |
| `-!2`   | Silence for 2, change all all length to 2 |
| `G3`    | Play G, octave 3 |
| `F#`    | Play F# |
| `F#3`   | Play F#, ocave 3 |
| `Bb`    | Play B flat |
| `Bb3`   | PLay B flat, octave 3 |

# Source:
All songs come from: https://lvkmusic.com.au/resources/easy-beginner-piano-songs/
