# GZip Test Assignment (Veeam)

The FSD is [here](TECH_TASK_CS_Compression_english_version.pdf).

## Format of archive

Archive consists of one or more chunks written consequentially.

Chunk format:

`[packed_len][orig_len][...gzipped_data...]`

whereas:

* `[packed_len]` - 4 bytes `int` storing the length of `[...gzipped_data...]`
* `[orig_len]` - 4 bytes `int` storing the source length (will be used to reserve enough amount of memory for the chunk decompression; may safely be larger than original length)
* `[...gzipped_data...]` - result of `System.IO.Compression.GZipStream`

### Special case of zero-length

If an input file is of 0 bytes, the output file will be also of 0 bytes. It can be safely fed to decompressor (decompression will result in, obviously, 0 bytes of outcome).

## Solution

### Compression

The input file is split to blocks (of controllable size, default is 1 MiB).

These blocks are distributed among threads in the such way (for a case of 3 threads and 7 blocks):

* Thread 0: block # 0, block # 3, block # 6
* Thread 1: block # 1, block # 4, block # 7
* Thread 2: block # 2, block # 5

This distribution guarantees that we can collect gzipped chunks enumerating threads' data by the order of threads, and write them down consequentially.

All threads' "columns" mentioned above are synchronized (barriered), i.e. thread 0 will not start gzipping of block # 3 until the main thread collects gzipped block # 0. (However once the main thread writes down block # 0, thread 0 immediately starts gzipping the block # 3 while main thread writes gzipped block # 1 from thread 1.)

The case of uneven distribution is handled safely: the threads with larger indices will complete earlier than others (due to less work on them), the code manages it.

#### Features

* Block size and *maximum* number of threads are controllable via CLI args. See usage of the binary.
* There is an option to print the distribution mentioned above via `/Verbose` CLI arg.

#### Caveats

* The solution relies onto `MemoryStream` backing for gzipping. It reads the block from the input file into RAM and feds it to GZip. That means the RAM usage is approx. `num_of_threads * block_size`.
  It should be possible to back the `GZipStream` with a custom-bounded file stream (with overridden `.Position`, `.Length` etc.), but I've chosen to play easily.
* Maximum block size is limited by maximum value of *signed* `int` (2 GiB). It doesn't make sense and could be of 4 GiB, but it's the limitation of `.Read()` method of .NET (3rd param `count` is of `Int32`).

### Decompression

The decompression is straight-forward and single-threaded. (As per my understanding of FSD the parallel work was required only for compression.)

I can make it parallel of course if needed, just will take more time.

#### Caveat

* The same `MemoryStream` backing.

## Architecture

* `Program.cs`
  Entry point.
* `ArgumentsCaptor.cs`
  Parses and stores CLI args. `Program` uses it to pass necessary arguments into necessary classes/methods.
* `FileWrapper.cs`
  Just an abstract class containing common disposal code for all file operator classes. Derived classes: `FileReader`, `PackedSerializer`.
* `FileReader.cs`
  Base abstract class for derived readers `UnpackedFileReader` and `PackedDeserializer`. Contains common code for opening file and detecting EOF.
* `UnpackedFileReader.cs`
  Block-reader of an input file. Usage: Java-style iterating of blocks.
* `PackedDeserializer.cs`
  Block-reader of an archive. Usage: similar to `UnpackedFileReader` but alongside with the gzipped chunk also returns the original length of the block before gzipping.
* `PackedSerializer.cs`
  Writes compressed (gzipped) chunks into archive according to the described format.
* `WorkDistributor.cs`
  Two aims: provides the optimal threads number (equal to number of cores of the machine), and generates array of thread-bound lists of blocks to work on.
* `MathUtils.cs`
  Just a method to divide two integers rounding up if necessary. (I.e. `1028 / 1024 = 2`, but `1024 / 1024 = 1`.)