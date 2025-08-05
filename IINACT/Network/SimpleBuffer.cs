namespace IINACT.Network;

internal class SimpleBuffer(int size)
{
	private readonly byte[] buffer = new byte[size];
	private int offset = 0;
    
    public int Size => offset;

    public Span<byte> Get(int start, int length)
    {
        return buffer.AsSpan()[start..(start + length)];
    }

    public void Write(ReadOnlySpan<byte> src)
	{
		if (offset + src.Length > buffer.Length)
			throw new ArgumentException("Src length must be less than the remaining size of the buffer.");
        
		var dstSlice = buffer.AsSpan().Slice(offset, src.Length);
		src.CopyTo(dstSlice);
		offset += src.Length;
	}
	
	public void WriteNull(int count)
	{
		if (offset + count > buffer.Length)
			throw new ArgumentException("Src length must be less than the remaining size of the buffer.");
		
		var dstSlice = buffer.AsSpan().Slice(offset, count);
		for (var i = 0; i < count; i++) dstSlice[i] = 0;
		offset += count;
	}

	public void Clear()
	{
		offset = 0;
	}

	public ReadOnlySpan<byte> GetBuffer()
	{
		return buffer.AsSpan()[..offset];
	}
}
