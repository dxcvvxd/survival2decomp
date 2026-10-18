using System.Collections.Generic;

namespace Engine.Graphics;

public sealed class FlatBatch3D : BaseFlatBatch
{
	public FlatBatch3D()
	{
		base.DepthStencilState = DepthStencilState.Default;
		base.RasterizerState = RasterizerState.CullNoneScissor;
		base.BlendState = BlendState.AlphaBlend;
	}

	public void QueueBatchTriangles(FlatBatch3D batch, Matrix? matrix = null, Color? color = null)
	{
		int count = TriangleVertices.Count;
		TriangleVertices.AddRange(batch.TriangleVertices);
		int count2 = TriangleIndices.Count;
		int count3 = batch.TriangleIndices.Count;
		TriangleIndices.Count += count3;
		for (int i = 0; i < count3; i++)
		{
			TriangleIndices[i + count2] = (ushort)(batch.TriangleIndices[i] + count);
		}
		if (matrix.HasValue && matrix != Matrix.Identity)
		{
			TransformTriangles(matrix.Value, count);
		}
		if (color.HasValue && color != Color.White)
		{
			TransformTrianglesColors(color.Value, count);
		}
	}

	public void QueueBatchLines(FlatBatch3D batch, Matrix? matrix = null, Color? color = null)
	{
		int count = LineVertices.Count;
		LineVertices.AddRange(batch.LineVertices);
		int count2 = LineIndices.Count;
		int count3 = batch.LineIndices.Count;
		LineIndices.Count += count3;
		for (int i = 0; i < count3; i++)
		{
			LineIndices[i + count2] = (ushort)(batch.LineIndices[i] + count);
		}
		if (matrix.HasValue && matrix != Matrix.Identity)
		{
			TransformLines(matrix.Value, count);
		}
		if (color.HasValue && color != Color.White)
		{
			TransformLinesColors(color.Value, count);
		}
	}

	public void QueueBatch(FlatBatch3D batch, Matrix? matrix = null, Color? color = null)
	{
		QueueBatchLines(batch, matrix, color);
		QueueBatchTriangles(batch, matrix, color);
	}

	public void QueueLine(Vector3 p1, Vector3 p2, Color color)
	{
		int count = LineVertices.Count;
		LineVertices.Count += 2;
		LineVertices.Array[count] = new VertexPositionColor(p1, color);
		LineVertices.Array[count + 1] = new VertexPositionColor(p2, color);
		int count2 = LineIndices.Count;
		LineIndices.Count += 2;
		LineIndices.Array[count2] = (ushort)count;
		LineIndices.Array[count2 + 1] = (ushort)(count + 1);
	}

	public void QueueLine(Vector3 p1, Vector3 p2, Color color1, Color color2)
	{
		int count = LineVertices.Count;
		LineVertices.Count += 2;
		LineVertices.Array[count] = new VertexPositionColor(p1, color1);
		LineVertices.Array[count + 1] = new VertexPositionColor(p2, color2);
		int count2 = LineIndices.Count;
		LineIndices.Count += 2;
		LineIndices.Array[count2] = (ushort)count;
		LineIndices.Array[count2 + 1] = (ushort)(count + 1);
	}

	public void QueueLineStrip(IEnumerable<Vector3> points, Color color)
	{
		int count = LineVertices.Count;
		int num = 0;
		foreach (Vector3 point in points)
		{
			LineVertices.Add(new VertexPositionColor(point, color));
			num++;
		}
		for (int i = 0; i < num - 1; i++)
		{
			LineIndices.Add((ushort)(count + i));
			LineIndices.Add((ushort)(count + i + 1));
		}
	}

	public void QueueTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color color)
	{
		int count = TriangleVertices.Count;
		TriangleVertices.Count += 3;
		TriangleVertices.Array[count] = new VertexPositionColor(p1, color);
		TriangleVertices.Array[count + 1] = new VertexPositionColor(p2, color);
		TriangleVertices.Array[count + 2] = new VertexPositionColor(p3, color);
		int count2 = TriangleIndices.Count;
		TriangleIndices.Count += 3;
		TriangleIndices.Array[count2] = (ushort)count;
		TriangleIndices.Array[count2 + 1] = (ushort)(count + 1);
		TriangleIndices.Array[count2 + 2] = (ushort)(count + 2);
	}

	public void QueueTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color color1, Color color2, Color color3)
	{
		int count = TriangleVertices.Count;
		TriangleVertices.Count += 3;
		TriangleVertices.Array[count] = new VertexPositionColor(p1, color1);
		TriangleVertices.Array[count + 1] = new VertexPositionColor(p2, color2);
		TriangleVertices.Array[count + 2] = new VertexPositionColor(p3, color3);
		int count2 = TriangleIndices.Count;
		TriangleIndices.Count += 3;
		TriangleIndices.Array[count2] = (ushort)count;
		TriangleIndices.Array[count2 + 1] = (ushort)(count + 1);
		TriangleIndices.Array[count2 + 2] = (ushort)(count + 2);
	}

	public void QueueQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color color)
	{
		int count = TriangleVertices.Count;
		TriangleVertices.Count += 4;
		TriangleVertices.Array[count] = new VertexPositionColor(p1, color);
		TriangleVertices.Array[count + 1] = new VertexPositionColor(p2, color);
		TriangleVertices.Array[count + 2] = new VertexPositionColor(p3, color);
		TriangleVertices.Array[count + 3] = new VertexPositionColor(p4, color);
		int count2 = TriangleIndices.Count;
		TriangleIndices.Count += 6;
		TriangleIndices.Array[count2] = (ushort)count;
		TriangleIndices.Array[count2 + 1] = (ushort)(count + 1);
		TriangleIndices.Array[count2 + 2] = (ushort)(count + 2);
		TriangleIndices.Array[count2 + 3] = (ushort)(count + 2);
		TriangleIndices.Array[count2 + 4] = (ushort)(count + 3);
		TriangleIndices.Array[count2 + 5] = (ushort)count;
	}

	public void QueueQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color color1, Color color2, Color color3, Color color4)
	{
		int count = TriangleVertices.Count;
		TriangleVertices.Count += 4;
		TriangleVertices.Array[count] = new VertexPositionColor(p1, color1);
		TriangleVertices.Array[count + 1] = new VertexPositionColor(p2, color2);
		TriangleVertices.Array[count + 2] = new VertexPositionColor(p3, color3);
		TriangleVertices.Array[count + 3] = new VertexPositionColor(p4, color4);
		int count2 = TriangleIndices.Count;
		TriangleIndices.Count += 6;
		TriangleIndices.Array[count2] = (ushort)count;
		TriangleIndices.Array[count2 + 1] = (ushort)(count + 1);
		TriangleIndices.Array[count2 + 2] = (ushort)(count + 2);
		TriangleIndices.Array[count2 + 3] = (ushort)(count + 2);
		TriangleIndices.Array[count2 + 4] = (ushort)(count + 3);
		TriangleIndices.Array[count2 + 5] = (ushort)count;
	}

	public void QueueBoundingBox(BoundingBox boundingBox, Color color)
	{
		QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z), new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z), color);
		QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z), new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Min.Z), color);
		QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Min.Z), new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Min.Z), color);
		QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Min.Z), new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z), color);
		QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.Z), new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z), color);
		QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z), new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z), color);
		QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z), new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Max.Z), color);
		QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Max.Z), new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.Z), color);
		QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z), new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.Z), color);
		QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Min.Z), new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Max.Z), color);
		QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Min.Z), new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z), color);
		QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z), new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z), color);
	}

	public void QueueBoundingFrustum(BoundingFrustum boundingFrustum, Color color)
	{
		ReadOnlyList<Vector3> corners = boundingFrustum.Corners;
		QueueLine(corners[0], corners[1], color);
		QueueLine(corners[1], corners[2], color);
		QueueLine(corners[2], corners[3], color);
		QueueLine(corners[3], corners[0], color);
		QueueLine(corners[4], corners[5], color);
		QueueLine(corners[5], corners[6], color);
		QueueLine(corners[6], corners[7], color);
		QueueLine(corners[7], corners[4], color);
		QueueLine(corners[0], corners[4], color);
		QueueLine(corners[1], corners[5], color);
		QueueLine(corners[2], corners[6], color);
		QueueLine(corners[3], corners[7], color);
	}
}
