cmake_minimum_required(VERSION 3.24)

project(ReboundForgeHooksRun LANGUAGES CXX)

if(CMAKE_CXX_COMPILER_ARCHITECTURE_ID STREQUAL "x64")
    set(ARCH x64)
elseif(CMAKE_CXX_COMPILER_ARCHITECTURE_ID STREQUAL "ARM64")
    set(ARCH ARM64)
else()
    message(FATAL_ERROR
        "Unsupported target architecture: ${CMAKE_CXX_COMPILER_ARCHITECTURE_ID}"
    )
endif()

set(MINHOOK_DETOURS_LIB
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/minhook-detours.${ARCH}.Release.lib"
)

if(NOT EXISTS "${MINHOOK_DETOURS_LIB}")
    message(FATAL_ERROR "MinHook library not found: ${MINHOOK_DETOURS_LIB}")
endif()

add_library(ReboundForgeHooksRun SHARED
    src/dllmain.cpp
    src/pch.cpp
)

target_compile_features(ReboundForgeHooksRun PRIVATE cxx_std_20)

target_compile_definitions(ReboundForgeHooksRun PRIVATE
    UNICODE
    _UNICODE
    REBOUNDFORGEHOOKSRUN_EXPORTS
    _WINDOWS
    _USRDLL
)

target_precompile_headers(ReboundForgeHooksRun PRIVATE
    src/pch.h
)

target_include_directories(ReboundForgeHooksRun PRIVATE
    src
    lib
)

target_link_libraries(ReboundForgeHooksRun PRIVATE
    "${MINHOOK_DETOURS_LIB}"
)

set_target_properties(ReboundForgeHooksRun PROPERTIES
    OUTPUT_NAME "Rebound.Forge.Hooks.Run.${ARCH}"
    PREFIX ""
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/bin"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/bin"
)