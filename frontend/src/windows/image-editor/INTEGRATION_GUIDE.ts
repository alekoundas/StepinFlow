// /**
//  * Image Editor Integration Example
//  *
//  * Shows how to integrate the image editor into your workflow/flowstep components.
//  *
//  * Example use cases:
//  * - IMAGE_SEARCH step: Select and edit template image
//  * - Custom image annotation/cropping
//  * - Template image refinement before saving
//  */

// import { ElectronApiService } from "@/shared/services/electron-api-service";
// import React from "react";

// /**
//  * Example: Open image editor from IMAGE_SEARCH FlowStep
//  *
//  * The user provides a template image for visual search.
//  * This function lets them refine the template in the editor.
//  */
// export async function editImageSearchTemplate(
//   currentImageBytes: Uint8Array | null,
//   stepId: string,
// ): Promise<{ success: boolean; editedImageBytes: Uint8Array | null }> {
//   try {
//     // Step 1: Convert image bytes to data URL (what editor expects)
//     // If no current image, create a blank one
//     let dataUrl: string;

//     if (currentImageBytes && currentImageBytes.length > 0) {
//       // Convert Uint8Array to base64
//       const binaryString = String.fromCharCode(...Array.from(currentImageBytes));
//       const base64 = btoa(binaryString);
//       dataUrl = `data:image/png;base64,${base64}`;
//     } else {
//       // Create blank PNG (1x1 transparent)
//       // In practice, you might show a canvas capture dialog first
//       dataUrl = createBlankPNG();
//     }

//     // Step 2: Open image editor window
//     // This returns when user clicks Export or closes window
//     const result = await ElectronApiService.imageEditor.openWindow(
//       dataUrl,
//       stepId, // Optional: track which step owns this edit
//     );

//     // Step 3: Handle result
//     if (result.pngBytes && result.pngBytes.length > 0) {
//       // User clicked Export - use edited image
//       console.log(
//         `[IMAGE_SEARCH] Template edited, new size: ${result.pngBytes.length} bytes`,
//       );
//       return {
//         success: true,
//         editedImageBytes: result.pngBytes,
//       };
//     } else {
//       // User canceled - return original
//       console.log("[IMAGE_SEARCH] Template edit canceled");
//       return {
//         success: false,
//         editedImageBytes: currentImageBytes,
//       };
//     }
//   } catch (error) {
//     console.error("[IMAGE_SEARCH] Error opening image editor:", error);
//     return {
//       success: false,
//       editedImageBytes: currentImageBytes,
//     };
//   }
// }

// /**
//  * Example: Use in a React component
//  *
//  * Shows how to integrate the editor call into a form or modal
//  */
// export function ImageSearchTemplateEditor({
//   currentImage,
//   onImageChange,
//   stepId,
// }: {
//   currentImage: Uint8Array | null;
//   onImageChange: (imageBytes: Uint8Array) => void;
//   stepId: string;
// }) {
//   const [isEditing, setIsEditing] = React.useState(false);

//   const handleEditTemplate = async () => {
//     setIsEditing(true);

//     try {
//       const result = await editImageSearchTemplate(currentImage, stepId);

//       if (result.success && result.editedImageBytes) {
//         onImageChange(result.editedImageBytes);
//       }
//     } finally {
//       setIsEditing(false);
//     }
//   };

//   return (<>
//     <div style={{ padding: "16px", border: "1px solid #ccc", borderRadius: 8 }}>
//       <h3>Template Image</h3>

//       {currentImage && currentImage.length > 0 ? (
//         <div>
//           <p>✓ Template image loaded ({currentImage.length} bytes)</p>
//           <img
//             src={uint8ArrayToDataURL(currentImage)}
//             alt="Template"
//             style={{ maxWidth: "200px", border: "1px solid #ddd" }}
//           />
//         </div>
//       ) : (
//         <p style={{ color: "#999" }}>No template image</p>
//       )}

//       <button
//         onClick={handleEditTemplate}
//         disabled={isEditing}
//         style={{
//           marginTop: "12px",
//           padding: "8px 16px",
//           background: "#669bff",
//           color: "white",
//           border: "none",
//           borderRadius: 4,
//           cursor: isEditing ? "not-allowed" : "pointer",
//           opacity: isEditing ? 0.6 : 1,
//         }}
//       >
//         {isEditing ? "Opening Editor..." : "✏️ Edit Template"}
//       </button>
//     </div>
//     </>
//   );
// }

// // ============================================================================
// // Helper Functions
// // ============================================================================

// /**
//  * Convert Uint8Array (PNG bytes) to data URL
//  * Used to display binary image data
//  */
// function uint8ArrayToDataURL(uint8arr: Uint8Array, mimeType = "image/png"): string {
//   let binary = "";
//   const bytes = new Uint8Array(uint8arr);
//   const len = bytes.byteLength;

//   for (let i = 0; i < len; i++) {
//     binary += String.fromCharCode(bytes[i]);
//   }

//   return `data:${mimeType};base64,${btoa(binary)}`;
// }

// /**
//  * Create a blank transparent 1x1 PNG
//  * Used as placeholder when no image provided
//  */
// function createBlankPNG(): string {
//   // This is a minimal 1x1 transparent PNG encoded as data URL
//   const blankPNG =
//     "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
//   return `data:image/png;base64,${blankPNG}`;
// }

// /**
//  * Convert File to Uint8Array
//  * Used when user selects image file from disk
//  */
// async function fileToUint8Array(file: File): Promise<Uint8Array> {
//   return new Promise((resolve, reject) => {
//     const reader = new FileReader();
//     reader.onload = () => {
//       resolve(new Uint8Array(reader.result as ArrayBuffer));
//     };
//     reader.onerror = reject;
//     reader.readAsArrayBuffer(file);
//   });
// }

// /**
//  * Convert File to data URL
//  * Used for previewing before editing
//  */
// async function fileToDataURL(file: File): Promise<string> {
//   return new Promise((resolve, reject) => {
//     const reader = new FileReader();
//     reader.onload = () => {
//       resolve(reader.result as string);
//     };
//     reader.onerror = reject;
//     reader.readAsDataURL(file);
//   });
// }

// // ============================================================================
// // Advanced Example: IMAGE_SEARCH FlowStep Integration
// // ============================================================================

// /**
//  * Typical IMAGE_SEARCH step form might look like:
//  *
//  * ```
//  * <Form>
//  *   <TextField label="Step Name" ... />
//  *   <Dropdown label="Search Area" options={searchAreas} ... />
//  *
//  *   <Section title="Template Image">
//  *     <ImageSearchTemplateEditor
//  *       currentImage={formData.templateImage}
//  *       onImageChange={(bytes) => setFormData({
//  *         ...formData,
//  *         templateImage: bytes
//  *       })}
//  *       stepId={stepId}
//  *     />
//  *   </Section>
//  *
//  *   <TextField label="Similarity Threshold" type="number" ... />
//  *   <Checkbox label="Scale Invariant" ... />
//  * </Form>
//  * ```
//  */

// // ============================================================================
// // Error Handling
// // ============================================================================

// export class ImageEditorError extends Error {
//   constructor(
//     public code: "EDITOR_NOT_AVAILABLE" | "INVALID_IMAGE" | "EXPORT_FAILED",
//     message: string,
//   ) {
//     super(message);
//     this.name = "ImageEditorError";
//   }
// }

// /**
//  * Open image editor with error handling
//  */
// export async function editImageSearchTemplateWithErrorHandling(
//   currentImageBytes: Uint8Array | null,
//   stepId: string,
// ): Promise<Uint8Array | null> {
//   try {
//     // Check if Electron API is available
//     if (!window.electronApi?.imageEditor) {
//       throw new ImageEditorError(
//         "EDITOR_NOT_AVAILABLE",
//         "Image editor not available (running outside Electron?)",
//       );
//     }

//     // Validate image data if provided
//     if (currentImageBytes && currentImageBytes.length < 8) {
//       throw new ImageEditorError(
//         "INVALID_IMAGE",
//         "Image data appears to be corrupted",
//       );
//     }

//     const result = await editImageSearchTemplate(currentImageBytes, stepId);

//     if (result.success && result.editedImageBytes) {
//       return result.editedImageBytes;
//     } else {
//       return currentImageBytes; // User canceled
//     }
//   } catch (error) {
//     if (error instanceof ImageEditorError) {
//       console.error(`[ImageEditor Error] ${error.code}: ${error.message}`);
//       // Show user-friendly error message
//       alert(`Could not open image editor: ${error.message}`);
//     } else {
//       console.error("[ImageEditor] Unexpected error:", error);
//       alert("An unexpected error occurred while opening the image editor");
//     }
//     return currentImageBytes; // Return unchanged on error
//   }
// }

// // ============================================================================
// // Data Validation
// // ============================================================================

// /**
//  * Validate that bytes are a valid PNG
//  */
// export function validatePNG(bytes: Uint8Array): boolean {
//   // PNG files start with signature: 89 50 4E 47 0D 0A 1A 0A
//   const pngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

//   if (bytes.length < 8) return false;

//   for (let i = 0; i < 8; i++) {
//     if (bytes[i] !== pngSignature[i]) return false;
//   }

//   return true;
// }

// /**
//  * Get image dimensions from PNG
//  * Useful for UI preview sizing
//  */
// export async function getPNGDimensions(
//   bytes: Uint8Array,
// ): Promise<{ width: number; height: number } | null> {
//   return new Promise((resolve) => {
//     const blob = new Blob([bytes], { type: "image/png" });
//     const url = URL.createObjectURL(blob);
//     const img = new Image();

//     img.onload = () => {
//       URL.revokeObjectURL(url);
//       resolve({ width: img.width, height: img.height });
//     };

//     img.onerror = () => {
//       URL.revokeObjectURL(url);
//       resolve(null);
//     };

//     img.src = url;
//   });
// }

// // ============================================================================
// // Caching Example
// // ============================================================================

// /**
//  * Cache edited images to avoid re-opening editor for same step
//  * Useful if users edit, cancel, then edit again
//  */
// const imageEditorCache = new Map<string, Uint8Array>();

// export function setCachedEditedImage(stepId: string, bytes: Uint8Array): void {
//   imageEditorCache.set(stepId, bytes);
// }

// export function getCachedEditedImage(stepId: string): Uint8Array | undefined {
//   return imageEditorCache.get(stepId);
// }

// export function clearImageEditorCache(stepId?: string): void {
//   if (stepId) {
//     imageEditorCache.delete(stepId);
//   } else {
//     imageEditorCache.clear();
//   }
// }

// // ============================================================================
// // Testing / Mocking
// // ============================================================================

// /**
//  * For testing without Electron
//  * Replace with this mock during unit tests
//  */
// export const createImageEditorMock = () => ({
//   openWindow: async (dataUrl: string, stepId?: string) => ({
//     pngBytes: new Uint8Array([137, 80, 78, 71, 13, 10, 26, 10]), // PNG header
//     stepId,
//   }),
//   onImageReady: (callback: any) => () => {},
//   returnResult: (result: any) => {},
//   signalReady: () => {},
// });
