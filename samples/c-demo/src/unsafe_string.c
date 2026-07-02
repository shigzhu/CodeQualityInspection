#include <stdio.h>
#include <string.h>

void unsafe_string_demo(char *dst)
{
    char buffer[32];
    gets(buffer);
    strcpy(dst, buffer);
    sprintf(buffer, "%s", dst);
}

void unsafe_string_demo_placeholder(void)
{
}
