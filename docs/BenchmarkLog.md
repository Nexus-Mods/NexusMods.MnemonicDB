
## Sorting Benchmarks

### Starting, sorting a node of Count * 18 datoms before optimizations
#### BaseLine
| Method | Count | SortOrder | Mean          | Error       | StdDev      | Gen0      | Gen1     | Gen2     | Allocated   |
|------- |------ |---------- |--------------:|------------:|------------:|----------:|---------:|---------:|------------:|
| Sort   | 10    | EATV      |      6.270 us |   0.0312 us |   0.0277 us |    0.6638 |        - |        - |    12.29 KB |
| Sort   | 10    | AETV      |      6.481 us |   0.0876 us |   0.0820 us |    0.6638 |        - |        - |    12.21 KB |
| Sort   | 10    | AVTE      |     20.821 us |   0.1951 us |   0.1825 us |    1.8616 |        - |        - |    34.55 KB |
| Sort   | 1024  | EATV      |    930.602 us |   4.1332 us |   3.8662 us |  135.7422 | 135.7422 | 135.7422 |   1306.2 KB |
| Sort   | 1024  | AETV      |    995.705 us |  10.1062 us |   9.4533 us |  135.7422 | 135.7422 | 135.7422 |   1306.2 KB |
| Sort   | 1024  | AVTE      |  5,114.956 us |  22.0422 us |  17.2091 us |  539.0625 | 179.6875 | 132.8125 |  8507.14 KB |
| Sort   | 8192  | EATV      |  7,685.343 us | 129.3241 us | 120.9699 us |  578.1250 | 304.6875 | 304.6875 | 10448.75 KB |
| Sort   | 8192  | AETV      |  8,974.757 us | 174.6224 us | 201.0954 us |  593.7500 | 328.1250 | 328.1250 | 10448.51 KB |
| Sort   | 8192  | AVTE      | 51,526.680 us | 138.1243 us | 122.4436 us | 4700.0000 | 400.0000 | 400.0000 | 85329.49 KB |

#### After not creating datoms for comparisons, use spans
| Method | Count | SortOrder | Mean          | Error       | StdDev      | Gen0      | Gen1     | Gen2     | Allocated   |
|------- |------ |---------- |--------------:|------------:|------------:|----------:|---------:|---------:|------------:|
| Sort   | 10    | EATV      |      5.768 us |   0.1061 us |   0.0992 us |    0.6638 |        - |        - |    12.29 KB |
| Sort   | 10    | AETV      |      5.732 us |   0.0539 us |   0.0504 us |    0.6638 |        - |        - |    12.21 KB |
| Sort   | 10    | AVTE      |     18.499 us |   0.3468 us |   0.3561 us |    1.8616 |        - |        - |    34.55 KB |
| Sort   | 1024  | EATV      |    904.229 us |  15.1354 us |  13.4171 us |  135.7422 | 135.7422 | 135.7422 |   1306.2 KB |
| Sort   | 1024  | AETV      |    939.972 us |   4.1067 us |   3.8414 us |  135.7422 | 135.7422 | 135.7422 |   1306.2 KB |
| Sort   | 1024  | AVTE      |  4,851.047 us |  31.1563 us |  29.1437 us |  539.0625 | 179.6875 | 132.8125 |  8507.14 KB |
| Sort   | 8192  | EATV      |  7,986.727 us |  82.5969 us |  73.2200 us |  562.5000 | 296.8750 | 296.8750 | 10448.62 KB |
| Sort   | 8192  | AETV      |  8,389.280 us | 161.0100 us | 185.4194 us |  562.5000 | 296.8750 | 296.8750 | 10448.62 KB |
| Sort   | 8192  | AVTE      | 48,455.739 us | 586.9106 us | 548.9965 us | 4700.0000 | 400.0000 | 400.0000 | 85329.49 KB |

#### After using SequenceCompareTo instead of string.Compare
| Method | Count | SortOrder | Mean          | Error       | StdDev      | Gen0     | Gen1     | Gen2     | Allocated  |
|------- |------ |---------- |--------------:|------------:|------------:|---------:|---------:|---------:|-----------:|
| Sort   | 10    | EATV      |      4.718 us |   0.0519 us |   0.0460 us |   0.3662 |        - |        - |    6.82 KB |
| Sort   | 10    | AETV      |      4.819 us |   0.0669 us |   0.0626 us |   0.3662 |        - |        - |    6.82 KB |
| Sort   | 10    | AVTE      |     12.218 us |   0.0851 us |   0.0755 us |   0.3662 |        - |        - |    6.82 KB |
| Sort   | 1024  | EATV      |    692.738 us |   6.6507 us |   6.2210 us | 135.7422 | 135.7422 | 135.7422 |   666.4 KB |
| Sort   | 1024  | AETV      |    751.316 us |   2.5610 us |   2.1385 us | 135.7422 | 135.7422 | 135.7422 |  666.36 KB |
| Sort   | 1024  | AVTE      |  3,018.782 us |  21.6165 us |  20.2201 us | 132.8125 | 132.8125 | 132.8125 |  666.36 KB |
| Sort   | 8192  | EATV      |  6,469.992 us |  23.6490 us |  20.9643 us | 382.8125 | 382.8125 | 382.8125 | 5328.92 KB |
| Sort   | 8192  | AETV      |  6,951.287 us |  64.1066 us |  59.9653 us | 437.5000 | 437.5000 | 437.5000 | 5328.75 KB |
| Sort   | 8192  | AVTE      | 31,693.220 us | 438.1465 us | 409.8425 us | 375.0000 | 375.0000 | 375.0000 | 5329.32 KB |

#### After adding an inline cache for mapping attributeIDs to value serializers
| Method | Count | SortOrder | Mean          | Error       | StdDev      | Gen0     | Gen1     | Gen2     | Allocated  |
|------- |------ |---------- |--------------:|------------:|------------:|---------:|---------:|---------:|-----------:|
| Sort   | 10    | EATV      |      4.603 us |   0.0873 us |   0.1459 us |   0.4044 |        - |        - |    7.51 KB |
| Sort   | 10    | AETV      |      4.736 us |   0.0935 us |   0.0874 us |   0.3738 |        - |        - |    6.88 KB |
| Sort   | 10    | AVTE      |     11.132 us |   0.0981 us |   0.0918 us |   0.3662 |        - |        - |    6.88 KB |
| Sort   | 1024  | EATV      |    710.307 us |   8.2342 us |   7.7023 us | 135.7422 | 135.7422 | 135.7422 |  762.36 KB |
| Sort   | 1024  | AETV      |    819.001 us |  11.9555 us |  10.5982 us | 135.7422 | 135.7422 | 135.7422 |  666.42 KB |
| Sort   | 1024  | AVTE      |  2,654.940 us |   9.2877 us |   8.2333 us | 132.8125 | 132.8125 | 132.8125 |  666.42 KB |
| Sort   | 8192  | EATV      |  6,633.710 us |  76.5107 us |  71.5681 us | 437.5000 | 398.4375 | 398.4375 | 6096.81 KB |
| Sort   | 8192  | AETV      |  7,492.636 us | 143.3817 us | 140.8200 us | 453.1250 | 453.1250 | 453.1250 | 5328.76 KB |
| Sort   | 8192  | AVTE      | 27,392.986 us | 522.2735 us | 580.5056 us | 406.2500 | 406.2500 | 406.2500 | 5328.51 KB |


## Binary Search

### Before Modifications
| Method       | Count | TxCount | SortOrder | Mean     | Error     | StdDev    |
|------------- |------ |-------- |---------- |---------:|----------:|----------:|
| BinarySearch | 1024  | 1024    | EATV      | 3.587 us | 0.0092 us | 0.0081 us |
| BinarySearch | 1024  | 1024    | AETV      | 1.069 us | 0.0083 us | 0.0077 us |
| BinarySearch | 1024  | 1024    | AVTE      | 6.652 us | 0.0254 us | 0.0237 us |

### Step2

Inlined the comparison function for EATV, only loads the values required for each comparison, never loads the V
(as it is always unique for a given EAT combination). Specialized the binary search code in the index nodes to use the
inlined data directly and not require loading the child nodes. 53x improvement in this case

| Method       | Count | TxCount | SortOrder | Mean        | Error     | StdDev    |
|------------- |------ |-------- |---------- |------------:|----------:|----------:|
| BinarySearch | 1024  | 1024    | EATV      |    67.25 ns |  0.631 ns |  0.591 ns |
| BinarySearch | 1024  | 1024    | AETV      |   969.01 ns |  2.388 ns |  2.117 ns |
| BinarySearch | 1024  | 1024    | AVTE      | 6,286.26 ns | 20.278 ns | 17.976 ns |

### Step3

The same, for the AVTE sort order 157x improvement

| Method       | Count | TxCount | SortOrder | Mean        | Error    | StdDev   |
|------------- |------ |-------- |---------- |------------:|---------:|---------:|
| BinarySearch | 1024  | 1024    | EATV      |    70.98 ns | 0.585 ns | 0.547 ns |
| BinarySearch | 1024  | 1024    | AETV      | 1,031.54 ns | 6.539 ns | 5.797 ns |
| BinarySearch | 1024  | 1024    | AVTE      |    40.19 ns | 0.091 ns | 0.071 ns |

### Step4

After adding back in value testing to ensure proper sorting, 5x decrease in the worst case

| Method       | Count | TxCount | SortOrder | Mean      | Error    | StdDev   |
|------------- |------ |-------- |---------- |----------:|---------:|---------:|
| BinarySearch | 1024  | 1024    | EATV      |  92.30 ns | 0.436 ns | 0.408 ns |
| BinarySearch | 1024  | 1024    | AETV      | 114.23 ns | 0.348 ns | 0.291 ns |
| BinarySearch | 1024  | 1024    | AVTE      | 204.30 ns | 0.699 ns | 0.653 ns |
