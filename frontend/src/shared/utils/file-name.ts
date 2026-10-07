const invalidCharacters = '<>:"/\\|?*';
const reservedName = /^(con|prn|aux|nul|com[0-9¹²³]|lpt[0-9¹²³])$/i;

const isInvalid = (character: string): boolean => {
  const code = character.charCodeAt(0);
  const isControl = code < 0x20 || (code >= 0x7f && code <= 0x9f);

  return isControl || invalidCharacters.includes(character);
};

/**
 * Why the name cannot be a file name, or null when it can. The rules of FileNameHelper in Core:
 * a flow's name becomes the file it exports to, and the repository it lands in is cloned onto
 * Windows, macOS and Linux, so Windows' rules, the strictest, are the ones kept.
 */
export const validateFileName = (name: string): string | null => {
  if (name.trim().length === 0) return "The name is empty.";

  if ([...name].some(isInvalid))
    return 'The name becomes a file name, so it can\'t contain < > : " / \\ | ? * or control characters.';

  if (name.startsWith(" ") || name.startsWith("."))
    return "The name becomes a file name, so it can't start with a space or a dot.";

  if (name.endsWith(" ") || name.endsWith("."))
    return "The name becomes a file name, so it can't end in a space or a dot.";

  // Windows reads a device name up to the first dot, so NUL.txt is NUL too.
  if (reservedName.test(name.split(".")[0].trimEnd()))
    return `"${name}" is a device name on Windows, so it can't be a file name.`;

  return null;
};
