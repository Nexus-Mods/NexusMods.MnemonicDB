using System.Text;
using FluentAssertions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using static NexusMods.MnemonicDB.Abstractions.ElementComparers.Utf8Comparer;

namespace NexusMods.MnemonicDB.Storage.Tests
{
    public unsafe class Utf8ComparerTests
    {
        // Helper to invoke the unmanaged compare
        private static int CompareUtf8(byte[] a, byte[] b)
        {
            fixed (byte* pa = a)
            fixed (byte* pb = b)
            {
                return Utf8Comparer.Utf8CaseInsensitiveCompare(pa, a.Length, pb, b.Length);
            }
        }

        
        [Theory]
        [InlineData("", "")]
        [InlineData("hello", "hello")]
        [InlineData("Hello", "heLLo")]
        [InlineData("abc", "abd")]
        [InlineData("Z", "a")]
        [InlineData("å", "Å")]         
        [InlineData("ß", "SS")]        
        [InlineData("Γειά", "γειΆ")]   
        [InlineData("こんにちは", "こんばんは")]
        [InlineData("😊", "😃")]
        public void KnownStrings_MatchOrdinalIgnoreCase(string a, string b)
        {
            // derive expected from .NET's OrdinalIgnoreCase
            var expected = Math.Sign(string.Compare(a, b, StringComparison.OrdinalIgnoreCase));

            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);

            var result = CompareUtf8(ba, bb);
            Math.Sign(result).Should().Be(expected);
        }

        [Fact]
        public void LongAsciiAndNonAscii_CompareAtVariousPositions()
        {
            var sb1 = new StringBuilder();
            for (var i = 0; i < 300; i++)
            {
                sb1.Append((char)('A' + (i % 26)));
                sb1.Append('π');
                sb1.Append('中');
                sb1.Append("🙂");
            }
            var s1 = sb1.ToString();

            var variants = new[]
            {
                "X" + s1.Substring(1),
                s1.Substring(0, s1.Length/2) + "Y" + s1.Substring(s1.Length/2 + 1),
                s1.Substring(0, s1.Length-1) + "Z"
            };

            foreach (var s2 in variants)
            {
                var expected = Math.Sign(string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase));
                var b1 = Encoding.UTF8.GetBytes(s1);
                var b2 = Encoding.UTF8.GetBytes(s2);

                var cmp = CompareUtf8(b1, b2);
                Math.Sign(cmp).Should().Be(expected);
            }
        }

        [Fact]
        public void RandomGenerativeTests_MatchOrdinalIgnoreCase()
        {
            var rnd = new Random(42);
            const int trials = 200;
            for (var t = 0; t < trials; t++)
            {
                var lenA = rnd.Next(0, 200);
                var lenB = rnd.Next(0, 200);
                var a = RandomString(rnd, lenA);
                var b = RandomString(rnd, lenB);

                var expected = Math.Sign(string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
                var ba = Encoding.UTF8.GetBytes(a);
                var bb = Encoding.UTF8.GetBytes(b);

                var cmp = CompareUtf8(ba, bb);
                Math.Sign(cmp).Should().Be(expected);
            }

            static string RandomString(Random rnd, int length)
            {
                var sb = new StringBuilder(length);
                for (var i = 0; i < length; i++)
                {
                    var choice = rnd.Next(0, 4);
                    if (choice == 0)
                    {
                        sb.Append((char)rnd.Next(0x20, 0x7E));
                    }
                    else if (choice == 1)
                    {
                        sb.Append((char)rnd.Next(0x00A0, 0x00FF));
                    }
                    else if (choice == 2)
                    {
                        sb.Append((char)rnd.Next(0x0370, 0x03FF));
                    }
                    else
                    {
                        var cp = 0x1F600 + rnd.Next(0, 80);
                        sb.Append(char.ConvertFromUtf32(cp));
                    }
                }
                return sb.ToString();
            }
        }
    }
}
