using System.Text;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Storage.Tests
{
    public class Utf8ComparerTests
    {
        // Helper to invoke the unmanaged compare
        private static unsafe int CompareUtf8(byte[] a, byte[] b)
        {
            fixed (byte* pa = a)
            fixed (byte* pb = b)
            {
                return Utf8Comparer.Utf8CaseInsensitiveCompare(pa, a.Length, pb, b.Length);
            }
        }

        
        [Test]
        [Arguments("", "")]
        [Arguments("hello", "hello")]
        [Arguments("Hello", "heLLo")]
        [Arguments("/_R", "/Ao")]
        [Arguments("abc", "abd")]
        [Arguments("Z", "a")]
        [Arguments("å", "Å")]         
        [Arguments("ß", "SS")]        
        [Arguments("Γειά", "γειΆ")]   
        [Arguments("こんにちは", "こんばんは")]
        [Arguments("😊", "😃")]
        public async Task KnownStrings_MatchOrdinalIgnoreCase(string a, string b)
        {
            // derive expected from .NET's OrdinalIgnoreCase
            var expected = Math.Sign(string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase));

            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);

            var result = CompareUtf8(ba, bb);
            await Assert.That(Math.Sign(result)).IsEqualTo(expected);
        }

        [Test]
        public async Task LongAsciiAndNonAscii_CompareAtVariousPositions()
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
                await Assert.That(cmp).IsEqualTo(expected);
            }
        }

    }
}
