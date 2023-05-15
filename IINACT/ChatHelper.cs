using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

// Original:
//      https://git.anna.lgbt/ascclemens/XivCommon/src/branch/main/XivCommon/Functions/Chat.cs

namespace IINACT;

/// <summary>
/// A class containing chat functionality
/// </summary>
public static class ChatHelper
{
	private static class Signatures
	{
		internal const string SendChat = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9";
		internal const string SanitiseString = "E8 ?? ?? ?? ?? EB 0A 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 8D 8D";
	}

	private unsafe delegate void ProcessChatBoxDelegate(UIModule* uiModule, IntPtr message, IntPtr unused, byte a4);

	private static ProcessChatBoxDelegate? ProcessChatBox { get; }

	private static readonly unsafe delegate* unmanaged<Utf8String*, int, IntPtr, void> _sanitiseString = null!;

	static ChatHelper()
	{
		if (DalamudApi.SigScanner.TryScanText(Signatures.SendChat, out var processChatBoxPtr))
		{
			ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(processChatBoxPtr);
		}

		unsafe
		{
			if (DalamudApi.SigScanner.TryScanText(Signatures.SanitiseString, out var sanitisePtr))
			{
				_sanitiseString = (delegate* unmanaged<Utf8String*, int, IntPtr, void>)sanitisePtr;
			}
		}
	}

	/// <summary>
	/// <para>
	/// Send a given message to the chat box. <b>This can send chat to the server.</b>
	/// </para>
	/// <para>
	/// <b>This method is unsafe.</b> This method does no checking on your input and
	/// may send content to the server that the normal client could not. You must
	/// verify what you're sending and handle content and length to properly use
	/// this.
	/// </para>
	/// </summary>
	/// <param name="message">Message to send</param>
	/// <exception cref="InvalidOperationException">If the signature for this function could not be found</exception>
	public static unsafe void SendMessageUnsafe(byte[] message)
	{

	}

	/// <summary>
	/// <para>
	/// Send a given message to the chat box. <b>This can send chat to the server.</b>
	/// </para>
	/// <para>
	/// This method is slightly less unsafe than <see cref="SendMessageUnsafe"/>. It
	/// will throw exceptions for certain inputs that the client can't normally send,
	/// but it is still possible to make mistakes. Use with caution.
	/// </para>
	/// </summary>
	/// <param name="message">message to send</param>
	/// <exception cref="ArgumentException">If <paramref name="message"/> is empty, longer than 500 bytes in UTF-8, or contains invalid characters.</exception>
	/// <exception cref="InvalidOperationException">If the signature for this function could not be found</exception>
	public unsafe static void SendMessage(string message)
	{
        var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
        var uiModule = framework->GetUiModule();

        using var payload = new ChatPayload(message);
        var payloadPtr = Marshal.AllocHGlobal(400);
        Marshal.StructureToPtr(payload, payloadPtr, false);

        ProcessChatBox(uiModule, payloadPtr, IntPtr.Zero, 0);

        Marshal.FreeHGlobal(payloadPtr);
    }

	/// <summary>
	/// <para>
	/// Sanitises a string by removing any invalid input.
	/// </para>
	/// <para>
	/// The result of this method is safe to use with
	/// <see cref="SendMessage"/>, provided that it is not empty or too
	/// long.
	/// </para>
	/// </summary>
	/// <param name="text">text to sanitise</param>
	/// <returns>sanitised text</returns>
	/// <exception cref="InvalidOperationException">If the signature for this function could not be found</exception>
	public static unsafe string SanitiseText(string text)
	{
		if (_sanitiseString == null)
		{
			throw new InvalidOperationException("Could not find signature for chat sanitisation");
		}

		var uText = Utf8String.FromString(text);

		_sanitiseString(uText, 0x27F, IntPtr.Zero);
		var sanitised = uText->ToString();

		uText->Dtor();
		IMemorySpace.Free(uText);

		return sanitised;
	}

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct ChatPayload : IDisposable
    {
        [FieldOffset(0)]
        private readonly IntPtr textPtr;

        [FieldOffset(16)]
        private readonly ulong textLen;

        [FieldOffset(8)]
        private readonly ulong unk1;

        [FieldOffset(24)]
        private readonly ulong unk2;

        internal ChatPayload(string text)
        {
            var stringBytes = Encoding.UTF8.GetBytes(text);
            this.textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);

            Marshal.Copy(stringBytes, 0, this.textPtr, stringBytes.Length);
            Marshal.WriteByte(this.textPtr + stringBytes.Length, 0);

            this.textLen = (ulong)(stringBytes.Length + 1);

            this.unk1 = 64;
            this.unk2 = 0;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(this.textPtr);
        }
    }
}
