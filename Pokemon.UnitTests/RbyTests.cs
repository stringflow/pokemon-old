using System.Linq;

using Xunit;

namespace Pokemon.UnitTests
{
    public class RbyTests
    {
        [Fact]
        public void TestCreateRed()
        {
            Red red = new();

            Assert.NotNull(red);
            Assert.True(red.Maps.Any());
            Assert.True(red.Species.Any());
        }

        [Fact]
        public void TestCreateBlue()
        {
            Blue blue = new();

            Assert.NotNull(blue);
            Assert.True(blue.Maps.Any());
            Assert.True(blue.Species.Any());
        }

        [Fact]
        public void TestCreateYellow()
        {
            Yellow yellow = new();

            Assert.NotNull(yellow);
            Assert.True(yellow.Maps.Any());
            Assert.True(yellow.Species.Any());
        }
    }
}
