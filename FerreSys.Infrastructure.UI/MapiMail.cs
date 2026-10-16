using System.Runtime.InteropServices;

namespace FerreSys.Infrastructure.UI;

internal enum MapiComposeResult
{
    Opened,
    Cancelled,
    Failed
}

internal static class MapiMail
{
    private const int Success = 0;
    private const int UserAbort = 1;
    private const int Dialog = 0x8;
    private const int LogonUi = 0x1;
    private const int RecipientTo = 1;

    public static MapiComposeResult TryCompose(IWin32Window owner, string to, string subject, string body, string attachmentPath)
    {
        var message = new MapiMessage
        {
            subject = subject,
            noteText = body
        };

        IntPtr recipients = IntPtr.Zero;
        IntPtr files = IntPtr.Zero;
        try
        {
            if (!string.IsNullOrWhiteSpace(to))
            {
                var recipient = new MapiRecipDesc
                {
                    recipClass = RecipientTo,
                    name = to.Trim(),
                    address = "SMTP:" + to.Trim()
                };
                recipients = Marshal.AllocHGlobal(Marshal.SizeOf<MapiRecipDesc>());
                Marshal.StructureToPtr(recipient, recipients, false);
                message.recipCount = 1;
                message.recips = recipients;
            }

            var file = new MapiFileDesc
            {
                position = -1,
                path = attachmentPath,
                name = Path.GetFileName(attachmentPath)
            };
            files = Marshal.AllocHGlobal(Marshal.SizeOf<MapiFileDesc>());
            Marshal.StructureToPtr(file, files, false);
            message.fileCount = 1;
            message.files = files;

            int result = MAPISendMail(IntPtr.Zero, owner.Handle, message, Dialog | LogonUi, 0);
            return result switch
            {
                Success => MapiComposeResult.Opened,
                UserAbort => MapiComposeResult.Cancelled,
                _ => MapiComposeResult.Failed
            };
        }
        catch (Exception)
        {
            return MapiComposeResult.Failed;
        }
        finally
        {
            if (recipients != IntPtr.Zero)
            {
                Marshal.DestroyStructure<MapiRecipDesc>(recipients);
                Marshal.FreeHGlobal(recipients);
            }

            if (files != IntPtr.Zero)
            {
                Marshal.DestroyStructure<MapiFileDesc>(files);
                Marshal.FreeHGlobal(files);
            }
        }
    }

    [DllImport("mapi32.dll", CharSet = CharSet.Ansi)]
    private static extern int MAPISendMail(IntPtr session, IntPtr uiParam, MapiMessage message, int flags, int reserved);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private sealed class MapiMessage
    {
        public int reserved;
        public string? subject;
        public string? noteText;
        public string? messageType;
        public string? dateReceived;
        public string? conversationId;
        public int flags;
        public IntPtr originator;
        public int recipCount;
        public IntPtr recips;
        public int fileCount;
        public IntPtr files;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private sealed class MapiRecipDesc
    {
        public int reserved;
        public int recipClass;
        public string? name;
        public string? address;
        public int eidSize;
        public IntPtr entryId;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private sealed class MapiFileDesc
    {
        public int reserved;
        public int flags;
        public int position;
        public string? path;
        public string? name;
        public IntPtr type;
    }
}
