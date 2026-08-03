using NUnit.Framework;
using UnityEngine;

namespace PhysicsSandbox.Tests
{
    // Compiling this project already proves the package's Runtime and Editor
    // assemblies build cleanly against a fresh Unity install of the resolved
    // dependencies (see TestProject~/Packages/manifest.json). This test just
    // gives the runner something to execute so a build failure shows up as a
    // clear red result instead of an ambiguous "no tests found".
    public class SmokeTests
    {
        [Test]
        public void PackageAssembliesLoad()
        {
            var go = new GameObject("SmokeTest", typeof(PhysicsBody));
            Assert.IsNotNull(go.GetComponent<PhysicsBody>());
            Object.DestroyImmediate(go);
        }
    }
}
