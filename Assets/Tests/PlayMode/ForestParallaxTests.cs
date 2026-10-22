using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DinoRush.Tests
{
    public sealed class ForestParallaxTests
    {
        [UnityTest]
        public IEnumerator AllLayersStayOverlappedAndCoverTheViewThroughMultipleWraps()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.StartRun();
            game.enabled = false; // Freeze time so render captures show the deliberately centered joins.
            try
            {
                var layers = (IList)typeof(ForestRunnerGame).GetField("layers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
                MethodInfo scroll = typeof(ForestRunnerGame).GetMethod("Scroll", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(layers.Count, Is.EqualTo(7));
                foreach (object layer in layers)
                {
                    System.Type type = layer.GetType();
                    var tiles = (Transform[])type.GetField("tiles").GetValue(layer);
                    float stride = (float)type.GetField("width").GetValue(layer);
                    float speed = (float)type.GetField("speed").GetValue(layer);
                    SpriteRenderer first = tiles[0].GetComponent<SpriteRenderer>();
                    Assert.That(first.sprite.name, Does.EndWith("_loop"));
                    float pixelWidth = tiles[0].lossyScale.x / first.sprite.pixelsPerUnit;
                    Assert.That(first.bounds.size.x - stride, Is.EqualTo(pixelWidth).Within(0.0001f));
                    foreach (float phase in new[] { 0f, 0.5f, 0.9999f, 1.0001f, 3.25f, 100.9999f })
                    {
                        type.GetField("offset").SetValue(layer, 0f);
                        scroll.Invoke(game, new object[] { phase * stride / speed });
                        float offset = (float)type.GetField("offset").GetValue(layer);
                        Assert.That(offset, Is.GreaterThanOrEqualTo(0).And.LessThan(stride));
                        for (int i = 1; i < tiles.Length; i++)
                        {
                            Bounds left = tiles[i - 1].GetComponent<SpriteRenderer>().bounds;
                            Bounds right = tiles[i].GetComponent<SpriteRenderer>().bounds;
                            Assert.That(left.max.x - right.min.x, Is.EqualTo(pixelWidth).Within(0.0002f),
                                first.name + " opened a gap at wrap phase " + phase);
                        }
                        Assert.That(tiles[0].GetComponent<SpriteRenderer>().bounds.min.x, Is.LessThan(-10.1f));
                        Assert.That(tiles[tiles.Length - 1].GetComponent<SpriteRenderer>().bounds.max.x, Is.GreaterThan(10.1f));
                    }
                }
                foreach (object layer in layers)
                {
                    System.Type type = layer.GetType();
                    type.GetField("offset").SetValue(layer, (float)type.GetField("width").GetValue(layer) * 0.5f);
                }
                scroll.Invoke(game, new object[] { 0f });
                yield return ForestRunTests.Capture("forest-loop-joins");
                yield return ForestRunTests.Capture("forest-loop-joins-tablet", 1024, 768);
                yield return ForestRunTests.Capture("forest-loop-joins-wide", 1920, 820);
            }
            finally { Object.Destroy(game.gameObject); }
            yield return null;
        }
    }
}
