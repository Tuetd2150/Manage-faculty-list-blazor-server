window.excelDownload = (fileName, contentType, base64Data) => {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${contentType};base64,${base64Data}`;
    link.click();
};
