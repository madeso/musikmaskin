using FluentAssertions;
using Xunit;
using MusikMaskin;

namespace test;

public class TestSynth
{
    [Fact]
    public void TestAdsr()
    {
        var env = new EnvelopeADSR(0.25, 0.25, 0.5, 0.5);

        // at start of attack
        env.AmplitudeAt(0, 0, null).Should().Be(0);

        // half into attack
        env.AmplitudeAt(0.125, 0, null).Should().Be(0.5);
        env.AmplitudeAt(1.125, 1, null).Should().Be(0.5);

        // height of attack
        env.AmplitudeAt(0.25, 0, null).Should().Be(1);
        env.AmplitudeAt(1.25, 1, null).Should().Be(1);

        // middle of decay
        env.AmplitudeAt(0.375, 0, null).Should().Be(0.75);
        env.AmplitudeAt(1.375, 1, null).Should().Be(0.75);

        // end of decay
        env.AmplitudeAt(0.5, 0, null).Should().Be(0.5);
    }

    [Fact]
    public void TestAlive()
    {
        var env = new EnvelopeADSR(0.25, 0.25, 0.5, 0.5);

        env.IsAlive(0, 0).Should().BeTrue();
        env.IsAlive(0.45, 0).Should().BeTrue();
        env.IsAlive(1, 0).Should().BeFalse();
        env.IsAlive(2, 0).Should().BeFalse();

        env.IsAlive(1, 1).Should().BeTrue();
        env.IsAlive(1.45, 1).Should().BeTrue();
        env.IsAlive(2, 1).Should().BeFalse();
        env.IsAlive(3, 1).Should().BeFalse();
    }
}