using System.Linq;

using Xunit;

namespace Pokemon.UnitTests
{
    public class GscTests
    {
        [Fact]
        public void TestCreateGold()
        {
            Gold gold = new();

            Assert.NotNull(gold);
            Assert.True(gold.Maps.Any());
            Assert.True(gold.Species.Any());
        }

        [Fact]
        public void TestCreateSilver()
        {
            Silver silver = new();

            Assert.NotNull(silver);
            Assert.True(silver.Maps.Any());
            Assert.True(silver.Species.Any());
        }

        [Fact]
        public void TestCreateCrystal()
        {
            Crystal crystal = new();

            Assert.NotNull(crystal);
            Assert.True(crystal.Maps.Any());
            Assert.True(crystal.Species.Any());
        }
    }
}
